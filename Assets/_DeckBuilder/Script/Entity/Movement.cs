using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MG.Extend;
using Unity.Netcode;

/// <summary>
/// Component allowing an entity to move with client-side prediction and server reconciliation.
/// Handles NavMeshAgent-based pathfinding with network synchronization.
/// Coordinates between specialized handlers for prediction, reconciliation, dash, and visual smoothing.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(GlobalStatSource))]
[RequireComponent(typeof(StatsModifierManager))]
[RequireComponent(typeof(AnimationHandler))]
public class Movement : NetworkBehaviour, IAbilityMovement, IInputReplayer
{
    #region Fields

    /// <summary>Data structure for dash movement configuration.</summary>
    [Serializable]
    public struct DashData
    {
        /// <summary>Distance to travel during the dash.</summary>
        [SerializeField]
        public float dashDistance;

        /// <summary>Speed of the dash movement.</summary>
        [SerializeField]
        public float dashSpeed;

        /// <summary>Whether to slide along walls when hitting them.</summary>
        [SerializeField]
        public bool slideOnWalls;

        /// <summary>Animation curve for dash easing.</summary>
        [SerializeField]
        public AnimationCurve dashCurve;

        /// <summary>Layer mask for blocking obstacles.</summary>
        [SerializeField]
        public LayerMask blockingMask;

        /// <summary>Maximum iterations for wall sliding calculation.</summary>
        [SerializeField]
        [Min(1)]
        public int maxWallIterations;
    }

    /// <summary>The stat key used to determine movement speed.</summary>
    [SerializeField]
    [Tooltip("The GlobalStatKey used to evaluate movement speed from stats.")]
    private GlobalStatKey movementSpeedStatKey = GlobalStatKey.MovementSpeed;

    /// <summary>Reference to the global stat source for speed evaluation.</summary>
    [SerializeField]
    [Tooltip("Reference to the GlobalStatSource component for speed evaluation.")]
    private GlobalStatSource globalStatSource;

    /// <summary>Reference to the stats modifier manager.</summary>
    [SerializeField]
    [Tooltip("Reference to the StatsModifierManager for speed modifier events.")]
    private StatsModifierManager modifierManager;

    /// <summary>Reference to the rigidbody for physics interactions.</summary>
    [SerializeField]
    [Tooltip("Reference to the Rigidbody component for physics interactions.")]
    private Rigidbody body;

    /// <summary>Reference to the NavMeshAgent for pathfinding.</summary>
    [SerializeField]
    [Tooltip("Reference to the NavMeshAgent component for pathfinding.")]
    private NavMeshAgent serverAgent;

    /// <summary>Reference to the capsule collider for collision detection.</summary>
    [SerializeField]
    [Tooltip("Reference to the CapsuleCollider for collision bounds.")]
    private CapsuleCollider capsuleCollider;

    /// <summary>Layer mask for ground detection.</summary>
    [SerializeField]
    [Tooltip("Layer mask used for ground detection during dash.")]
    private LayerMask groundLayerMask;

    /// <summary>Default dash configuration.</summary>
    [SerializeField]
    [Tooltip("Default dash configuration used when no specific data is provided.")]
    private DashData defaultDashData = new DashData();

    [Header("Animation")]
    /// <summary>Reference to the animation handler for movement animations.</summary>
    [SerializeField]
    [Tooltip("Reference to the AnimationHandler for movement animations.")]
    private AnimationHandler animationHandler;

    [Header("Network Prediction")]
    /// <summary>Transform used for visual interpolation, separate from simulation.</summary>
    [SerializeField]
    [Tooltip("Optional separate transform for visual representation. If null, uses this transform.")]
    private Transform visualTransform;

    /// <summary>Rate at which visual corrections are smoothed.</summary>
    [SerializeField]
    [Tooltip("How fast the visual catches up to the simulation position.")]
    private float correctionSmoothingRate = 10f;

    /// <summary>Threshold beyond which position snaps instead of interpolates.</summary>
    [SerializeField]
    [Tooltip("If position error exceeds this, snap instead of smooth.")]
    private float snapThreshold = 2f;

    /// <summary>How often the server broadcasts state updates.</summary>
    [SerializeField]
    [Tooltip("Server state broadcast interval in seconds.")]
    private float stateBroadcastInterval = 0.05f;

    internal const int DEFAULT_MAX_WALL_ITERATIONS = 100;
    private const float LARGE_ERROR_THRESHOLD = 1f;
    private const int MAX_CONSECUTIVE_LARGE_CORRECTIONS = 5;
    private const float MIN_ERROR_THRESHOLD = 0.01f;

    #endregion

    #region Private Members

    private bool _canMove = true;
    private Coroutine _dashRoutine;
    private float _baseMaxSpeed;
    private bool _isDashing;
    private uint _lastAcknowledgedSequence;
    private MovementState _lastReceivedState;

    private MovementPredictionHandler _predictionHandler;
    private MovementReconciliationHandler _reconciliationHandler;
    private MovementNetworkBroadcaster _networkBroadcaster;
    private DashHandler _dashHandler;
    private MovementVisualHandler _visualHandler;

    private readonly InterpolationBuffer _interpolationBuffer = new InterpolationBuffer();
    private readonly PositionHistory _positionHistory = new PositionHistory();

    private ReconciliationConfig _reconciliationConfig;

    #endregion

    #region Getters

    /// <summary>Gets the NavMeshAgent component.</summary>
    public NavMeshAgent Agent => serverAgent;

    /// <summary>Gets whether movement is currently allowed.</summary>
    public bool CanMove => _canMove;

    /// <summary>Gets whether the entity is currently moving.</summary>
    public bool IsMoving => serverAgent != null && serverAgent.velocity.magnitude > 0.1f;

    /// <summary>Gets whether the entity is currently dashing.</summary>
    public bool IsDashing => _isDashing;

    /// <summary>Gets the rigidbody component.</summary>
    public Rigidbody Body => body;

    /// <summary>Gets the position history for lag compensation.</summary>
    public PositionHistory PositionHistory => _positionHistory;

    /// <summary>Gets the interpolation buffer for non-owner rendering.</summary>
    public InterpolationBuffer InterpolationBuffer => _interpolationBuffer;

    private float AgentRadius => NavMesh.GetSettingsByID(serverAgent.agentTypeID).agentRadius;

    #endregion

    #region Unity Message Methods

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
        serverAgent = GetComponent<NavMeshAgent>();
        globalStatSource = GetComponent<GlobalStatSource>();
        modifierManager = GetComponent<StatsModifierManager>();
        animationHandler = GetComponent<AnimationHandler>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void Awake()
    {
        InitializeHandlers();
        InitializeMovementSpeed();
    }

    private void OnEnable()
    {
        SubscribeToModifierChanges();
        RefreshMovementSpeedFromStats();
    }

    private void OnDisable()
    {
        UnsubscribeFromModifierChanges();
    }

    private void OnValidate()
    {
        if (defaultDashData.maxWallIterations <= 0)
        {
            defaultDashData.maxWallIterations = DEFAULT_MAX_WALL_ITERATIONS;
        }
    }

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsOwner)
        {
            UpdateOwnerVisual();
        }
        else if (!IsServer)
        {
            UpdateNonOwnerInterpolation();
        }

        UpdateAnimationFromMovement();
    }

    private void FixedUpdate()
    {
        if (!IsSpawned)
        {
            return;
        }

        if (IsServer)
        {
            UpdateServerSimulation();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ConfigureNetworkRole();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        ResetHandlers();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Attempts to move the entity to the specified world position.
    /// </summary>
    /// <param name="worldTo">Target world position.</param>
    /// <returns>True if the movement request was accepted; otherwise false.</returns>
    public bool MoveTo(Vector3 worldTo)
    {
        if (!IsOwner)
        {
            return false;
        }

        if (!_canMove)
        {
            return false;
        }

        MovementInput input = _predictionHandler.CreateClickToMoveInput(worldTo, GetNetworkTime());
        ApplyLocalPrediction(input);
        SendInputToServer(input);

        return true;
    }

    /// <summary>
    /// Stops all movement immediately.
    /// </summary>
    public void StopMovement()
    {
        if (!serverAgent.enabled)
        {
            return;
        }

        serverAgent.ResetPath();
        serverAgent.velocity = Vector3.zero;

        if (IsOwner)
        {
            MovementInput input = _predictionHandler.CreateStopInput(GetNetworkTime());
            SendInputToServer(input);
        }
    }

    /// <summary>
    /// Disables movement capability.
    /// </summary>
    public void DisableMovement()
    {
        StopMovement();
        _canMove = false;
    }

    /// <summary>
    /// Enables movement capability.
    /// </summary>
    public void EnableMovement()
    {
        _canMove = true;
    }

    /// <summary>
    /// Performs a dash in the forward direction using default dash data.
    /// </summary>
    public void Dash()
    {
        Dash(defaultDashData, transform.position + transform.forward);
    }

    /// <summary>
    /// Performs a dash toward the specified position using default dash data.
    /// </summary>
    /// <param name="toward">Target position to dash toward.</param>
    public void Dash(Vector3 toward)
    {
        Dash(defaultDashData, toward);
    }

    /// <summary>
    /// Performs a dash in the forward direction using specified dash data.
    /// </summary>
    /// <param name="dashData">Configuration for the dash.</param>
    public void Dash(DashData dashData)
    {
        Dash(dashData, transform.position + transform.forward);
    }

    /// <summary>
    /// Performs a dash toward the specified position using specified dash data.
    /// </summary>
    /// <param name="dashData">Configuration for the dash.</param>
    /// <param name="toward">Target position to dash toward.</param>
    public void Dash(DashData dashData, Vector3 toward)
    {
        if (!IsOwner && !IsServer)
        {
            return;
        }

        StopMovement();
        StopExistingDashRoutine();

        List<Vector3> dashPositions = _dashHandler.ComputeDashPositions(
            dashData,
            transform.position,
            toward,
            serverAgent,
            groundLayerMask,
            AgentRadius
        );

        StartDash(dashPositions, dashData);
        SendDashInputIfOwner(dashData, toward);
    }

    /// <summary>
    /// Re-evaluates movement speed using the configured global stat.
    /// </summary>
    public void RefreshMovementSpeedFromStats()
    {
        float globalSpeed = GetGlobalMovementSpeed();
        if (globalSpeed > 0f)
        {
            _baseMaxSpeed = globalSpeed;
        }

        ApplyMovementSpeedToAgent();
    }

    /// <summary>
    /// Called when server state is received for reconciliation.
    /// </summary>
    /// <param name="state">The authoritative server state.</param>
    public void OnServerStateReceived(MovementState state)
    {
        _lastReceivedState = state;

        if (IsOwner)
        {
            ReconcileWithServerState(state);
        }
        else if (!IsServer)
        {
            _interpolationBuffer.AddSnapshot(state);
        }
    }

    #endregion

    #region IInputReplayer Implementation

    /// <summary>
    /// Replays a click-to-move input by setting the NavMeshAgent destination.
    /// </summary>
    /// <param name="targetPosition">The world position to move toward.</param>
    public void ReplayClickToMove(Vector3 targetPosition)
    {
        if (serverAgent.enabled && _canMove)
        {
            serverAgent.SetDestination(targetPosition);
        }
    }

    /// <summary>
    /// Replays a directional movement input.
    /// </summary>
    /// <param name="direction">The normalized movement direction.</param>
    /// <param name="speed">The movement speed.</param>
    /// <param name="deltaTime">The time delta for this movement step.</param>
    public void ReplayDirectional(Vector3 direction, float speed, float deltaTime)
    {
        if (serverAgent.enabled && _canMove)
        {
            serverAgent.Move(direction * speed * deltaTime);
        }
    }

    /// <summary>
    /// Replays a dash input by teleporting to the dash end position.
    /// </summary>
    /// <param name="dashData">The dash input data containing start position, direction, and distance.</param>
    public void ReplayDash(DashInputData dashData)
    {
        Vector3 dashEnd = dashData.StartPosition + dashData.Direction * dashData.Distance;
        transform.position = dashEnd;
    }

    /// <summary>
    /// Replays a stop input by resetting the path and velocity.
    /// </summary>
    public void ReplayStop()
    {
        if (serverAgent.enabled)
        {
            serverAgent.ResetPath();
            serverAgent.velocity = Vector3.zero;
        }
    }

    #endregion

    #region Private Methods

    private void InitializeHandlers()
    {
        _predictionHandler = new MovementPredictionHandler();
        _reconciliationHandler = new MovementReconciliationHandler();
        _networkBroadcaster = new MovementNetworkBroadcaster();
        _dashHandler = new DashHandler();
        _visualHandler = new MovementVisualHandler();

        _reconciliationConfig = new ReconciliationConfig
        {
            SnapThreshold = snapThreshold,
            LargeErrorThreshold = LARGE_ERROR_THRESHOLD,
            MaxConsecutiveLargeCorrections = MAX_CONSECUTIVE_LARGE_CORRECTIONS,
            MinErrorThreshold = MIN_ERROR_THRESHOLD
        };
    }

    private void InitializeMovementSpeed()
    {
        _baseMaxSpeed = serverAgent != null ? serverAgent.speed : 0f;
        RefreshMovementSpeedFromStats();
    }

    private void ResetHandlers()
    {
        _predictionHandler.Reset();
        _reconciliationHandler.Reset();
        _networkBroadcaster.Reset();
        _visualHandler.Reset();
        _interpolationBuffer.Clear();
        _positionHistory.Clear();
    }

    private void ConfigureNetworkRole()
    {
        if (IsOwner)
        {
            ConfigureAsOwner();
        }
        else if (IsServer)
        {
            ConfigureAsServer();
        }
        else
        {
            ConfigureAsNonOwnerClient();
        }
    }

    private void ConfigureAsOwner()
    {
        serverAgent.enabled = true;
        serverAgent.updatePosition = true;
        serverAgent.updateRotation = true;
    }

    private void ConfigureAsServer()
    {
        serverAgent.enabled = true;
        serverAgent.updatePosition = true;
        serverAgent.updateRotation = true;
    }

    private void ConfigureAsNonOwnerClient()
    {
        serverAgent.enabled = false;
    }

    private float GetNetworkTime()
    {
        if (NetworkManager.Singleton != null)
        {
            return (float)NetworkManager.Singleton.ServerTime.Time;
        }
        return Time.time;
    }

    private void ApplyLocalPrediction(MovementInput input)
    {
        if (!serverAgent.enabled)
        {
            return;
        }

        switch (input.InputType)
        {
            case MovementInputType.ClickToMove:
                serverAgent.SetDestination(input.TargetPosition);
                break;

            case MovementInputType.Directional:
                serverAgent.Move(input.MoveDirection * serverAgent.speed * Time.deltaTime);
                break;

            case MovementInputType.Stop:
                serverAgent.ResetPath();
                serverAgent.velocity = Vector3.zero;
                break;
        }
    }

    private void SendInputToServer(MovementInput input)
    {
        SendMovementInputServerRpc(input);
    }

    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    private void SendMovementInputServerRpc(MovementInput input)
    {
        ProcessInputOnServer(input);
    }

    private void ProcessInputOnServer(MovementInput input)
    {
        if (!serverAgent.enabled)
        {
            return;
        }

        if (!ValidateInput(input))
        {
            return;
        }

        ExecuteInputOnServer(input);
        _lastAcknowledgedSequence = input.SequenceNumber;
    }

    private void ExecuteInputOnServer(MovementInput input)
    {
        switch (input.InputType)
        {
            case MovementInputType.ClickToMove:
                if (_canMove)
                {
                    serverAgent.SetDestination(input.TargetPosition);
                }
                break;

            case MovementInputType.Directional:
                if (_canMove)
                {
                    serverAgent.Move(input.MoveDirection * serverAgent.speed * Time.fixedDeltaTime);
                }
                break;

            case MovementInputType.Stop:
                serverAgent.ResetPath();
                serverAgent.velocity = Vector3.zero;
                break;

            case MovementInputType.Dash:
                break;
        }
    }

    private bool ValidateInput(MovementInput input)
    {
        if (input.InputType == MovementInputType.ClickToMove)
        {
            if (!NavMesh.SamplePosition(input.TargetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateServerSimulation()
    {
        RecordPositionHistory();
        BroadcastStateIfNeeded();
    }

    private void RecordPositionHistory()
    {
        float currentTime = GetNetworkTime();
        _positionHistory.RecordPosition(
            currentTime,
            transform.position,
            transform.rotation,
            CalculateHitboxBounds()
        );
    }

    private void BroadcastStateIfNeeded()
    {
        float currentTime = GetNetworkTime();
        if (_networkBroadcaster.ShouldBroadcast(currentTime, stateBroadcastInterval))
        {
            BroadcastState(currentTime);
        }
    }

    private Bounds CalculateHitboxBounds()
    {
        if (capsuleCollider != null)
        {
            return capsuleCollider.bounds;
        }
        return new Bounds(transform.position, Vector3.one);
    }

    private void BroadcastState(float currentTime)
    {
        MovementState state = _networkBroadcaster.CreateMovementState(
            _lastAcknowledgedSequence,
            currentTime,
            transform.position,
            serverAgent.velocity,
            transform.rotation,
            IsMoving,
            _canMove,
            _isDashing,
            serverAgent.destination
        );

        BroadcastMovementStateClientRpc(state);
        _networkBroadcaster.RecordBroadcast(currentTime, transform.position);
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Unreliable)]
    private void BroadcastMovementStateClientRpc(MovementState state)
    {
        OnServerStateReceived(state);
    }

    private void ReconcileWithServerState(MovementState state)
    {
        _predictionHandler.RemoveAcknowledgedInputs(state.LastProcessedSequence);
        HandleCanMoveStateChange(state);

        if (_isDashing)
        {
            return;
        }

        PerformReconciliation(state);
    }

    private void HandleCanMoveStateChange(MovementState state)
    {
        if (!state.CanMove && _canMove)
        {
            _predictionHandler.ClearPendingInputs();
            StopMovement();
        }
        _canMove = state.CanMove;
    }

    private void PerformReconciliation(MovementState state)
    {
        Vector3 previousVisualPosition = _visualHandler.GetVisualPosition(transform.position);

        ReconciliationResult result = _reconciliationHandler.Reconcile(
            state,
            transform.position,
            previousVisualPosition,
            _reconciliationConfig
        );

        ApplyReconciliationResult(result);
    }

    private void ApplyReconciliationResult(ReconciliationResult result)
    {
        if (result.ShouldSnap)
        {
            ApplySnapCorrection(result);
        }
        else if (result.ShouldSmooth)
        {
            ApplySmoothCorrection(result);
        }
    }

    private void ApplySnapCorrection(ReconciliationResult result)
    {
        transform.position = result.CorrectedPosition;
        _visualHandler.ResetCorrectionOffset();

        if (result.ShouldClearInputs)
        {
            _predictionHandler.ClearPendingInputs();
        }
    }

    private void ApplySmoothCorrection(ReconciliationResult result)
    {
        Vector3 previousVisualPosition = _visualHandler.GetVisualPosition(transform.position);
        transform.position = result.CorrectedPosition;

        if (result.ShouldReplay)
        {
            _predictionHandler.ReplayPendingInputs(this, serverAgent.speed, Time.fixedDeltaTime);
        }

        _visualHandler.ApplyCorrectionOffset(previousVisualPosition, transform.position);
        _visualHandler.UpdateVisualPositionImmediate(visualTransform, transform.position, transform.rotation);
    }

    private void UpdateOwnerVisual()
    {
        if (_isDashing)
        {
            _visualHandler.SyncVisualToSimulation(visualTransform, transform.position, transform.rotation);
            return;
        }

        _visualHandler.UpdateOwnerVisual(
            visualTransform,
            transform.position,
            transform.rotation,
            correctionSmoothingRate,
            Time.deltaTime
        );
    }

    private void UpdateNonOwnerInterpolation()
    {
        float networkTime = GetNetworkTime();
        PositionSnapshot snapshot = _interpolationBuffer.GetInterpolatedSnapshot(networkTime);

        if (snapshot.Timestamp > 0f)
        {
            ApplyInterpolatedSnapshot(snapshot);
        }

        _interpolationBuffer.PruneOldSnapshots(networkTime);
    }

    private void ApplyInterpolatedSnapshot(PositionSnapshot snapshot)
    {
        transform.position = snapshot.Position;
        transform.rotation = snapshot.Rotation;

        if (visualTransform != null)
        {
            visualTransform.position = snapshot.Position;
            visualTransform.rotation = snapshot.Rotation;
        }
    }

    private void UpdateAnimationFromMovement()
    {
        if (animationHandler == null)
        {
            return;
        }

        Vector3 velocity = GetCurrentVelocity();
        animationHandler.UpdateMovement(velocity);
    }

    private Vector3 GetCurrentVelocity()
    {
        if (serverAgent != null && serverAgent.enabled)
        {
            return serverAgent.velocity;
        }
        return _lastReceivedState.Velocity;
    }

    private void StopExistingDashRoutine()
    {
        if (_dashRoutine != null)
        {
            StopCoroutine(_dashRoutine);
        }
    }

    private void StartDash(List<Vector3> dashPositions, DashData dashData)
    {
        _canMove = false;
        _isDashing = true;
        _visualHandler.ResetCorrectionOffset();
        _dashRoutine = StartCoroutine(DashRoutine(dashPositions, dashData));
    }

    private void SendDashInputIfOwner(DashData dashData, Vector3 toward)
    {
        if (!IsOwner)
        {
            return;
        }

        DashInputData dashInputData = new DashInputData
        {
            StartPosition = transform.position,
            Direction = (toward - transform.position).normalized,
            Distance = dashData.dashDistance,
            Speed = dashData.dashSpeed
        };

        MovementInput input = _predictionHandler.CreateDashInput(dashInputData, GetNetworkTime());
        SendInputToServer(input);
    }

    private IEnumerator DashRoutine(List<Vector3> dashPositions, DashData dashData)
    {
        SetBodyDashRotation(dashPositions);

        float dashDuration = _dashHandler.CalculateDashDuration(dashData.dashDistance, dashData.dashSpeed);
        float elapsedTime = 0f;

        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / dashDuration);
            float curvePosition = dashData.dashCurve.Evaluate(normalizedTime);

            Vector3 nextPosition = _dashHandler.InterpolatePath(dashPositions, curvePosition);
            serverAgent.nextPosition = nextPosition;

            yield return null;
        }

        CompleteDash();
    }

    private void SetBodyDashRotation(List<Vector3> dashPositions)
    {
        Quaternion lookRot = _dashHandler.CalculateDashRotation(
            transform.position,
            dashPositions[0],
            transform.forward
        );
        body.rotation = lookRot;
    }

    private void CompleteDash()
    {
        serverAgent.enabled = true;
        _canMove = true;
        _isDashing = false;
    }

    private float GetGlobalMovementSpeed()
    {
        if (globalStatSource == null || serverAgent == null)
        {
            return 0f;
        }

        Dictionary<GlobalStatKey, float> stats = globalStatSource.EvaluateGlobalStats();
        if (stats != null && stats.TryGetValue(movementSpeedStatKey, out float modified))
        {
            return modified;
        }

        Dictionary<GlobalStatKey, float> raw = globalStatSource.EvaluateGlobalStatsRaw();
        if (raw != null && raw.TryGetValue(movementSpeedStatKey, out float baseVal))
        {
            return baseVal;
        }

        return serverAgent.speed;
    }

    private void ApplyMovementSpeedToAgent()
    {
        if (serverAgent != null && _baseMaxSpeed > 0f)
        {
            serverAgent.speed = _baseMaxSpeed;
        }
    }

    private void SubscribeToModifierChanges()
    {
        if (modifierManager == null)
        {
            return;
        }

        modifierManager.OnGlobalModifiersChanged -= RefreshMovementSpeedFromStats;
        modifierManager.OnGlobalModifiersChanged += RefreshMovementSpeedFromStats;
        modifierManager.OnAnyModifiersChanged -= RefreshMovementSpeedFromStats;
        modifierManager.OnAnyModifiersChanged += RefreshMovementSpeedFromStats;
    }

    private void UnsubscribeFromModifierChanges()
    {
        if (modifierManager == null)
        {
            return;
        }

        modifierManager.OnGlobalModifiersChanged -= RefreshMovementSpeedFromStats;
        modifierManager.OnAnyModifiersChanged -= RefreshMovementSpeedFromStats;
    }

    #endregion
}
