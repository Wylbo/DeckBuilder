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
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(GlobalStatSource))]
[RequireComponent(typeof(StatsModifierManager))]
[RequireComponent(typeof(AnimationHandler))]
public class Movement : NetworkBehaviour, IAbilityMovement
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
    private GlobalStatKey movementSpeedStatKey = GlobalStatKey.MovementSpeed;

    /// <summary>Reference to the global stat source for speed evaluation.</summary>
    [SerializeField]
    private GlobalStatSource globalStatSource;

    /// <summary>Reference to the stats modifier manager.</summary>
    [SerializeField]
    private StatsModifierManager modifierManager;

    /// <summary>Reference to the rigidbody for physics interactions.</summary>
    [SerializeField]
    private Rigidbody body;

    /// <summary>Reference to the NavMeshAgent for pathfinding.</summary>
    [SerializeField]
    private NavMeshAgent serverAgent;

    /// <summary>Reference to the capsule collider for collision detection.</summary>
    [SerializeField]
    private CapsuleCollider capsuleCollider;

    /// <summary>Layer mask for ground detection.</summary>
    [SerializeField]
    private LayerMask groundLayerMask;

    /// <summary>Default dash configuration.</summary>
    [SerializeField]
    private DashData defaultDashData = new DashData();

    [Header("Animation")]
    /// <summary>Reference to the animation handler for movement animations.</summary>
    [SerializeField]
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
    private const int MAX_PENDING_INPUTS = 64;
    private const int MAX_CONSECUTIVE_LARGE_CORRECTIONS = 5;
    private const float LARGE_ERROR_THRESHOLD = 1f;

    #endregion

    #region Private Members

    private bool _canMove = true;
    private Coroutine _dashRoutine;
    private float _baseMaxSpeed;
    private bool _isDashing;

    private uint _nextSequenceNumber;
    private readonly Queue<MovementInput> _pendingInputs = new Queue<MovementInput>();
    private uint _lastAcknowledgedSequence;

    private Vector3 _correctionOffset = Vector3.zero;
    private int _consecutiveLargeCorrections;

    private readonly InterpolationBuffer _interpolationBuffer = new InterpolationBuffer();
    private readonly PositionHistory _positionHistory = new PositionHistory();

    private float _lastStateBroadcastTime;
    private Vector3 _lastBroadcastPosition;
    private MovementState _lastReceivedState;

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
    private float StepHeight => NavMesh.GetSettingsByID(serverAgent.agentTypeID).agentClimb;
    private float HalfHeight => capsuleCollider != null ? capsuleCollider.height / 2 : 1f;

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
        _baseMaxSpeed = serverAgent != null ? serverAgent.speed : 0f;
        RefreshMovementSpeedFromStats();
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

        _pendingInputs.Clear();
        _interpolationBuffer.Clear();
        _positionHistory.Clear();
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

        MovementInput input = CreateClickToMoveInput(worldTo);
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
            MovementInput input = CreateStopInput();
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

        if (_dashRoutine != null)
        {
            StopCoroutine(_dashRoutine);
        }

        List<Vector3> dashPositions = ComputeDashPositions(dashData, toward);

        _canMove = false;
        _isDashing = true;
        _dashRoutine = StartCoroutine(DashRoutine(dashPositions, dashData));

        if (IsOwner)
        {
            DashInputData dashInputData = new DashInputData
            {
                StartPosition = transform.position,
                Direction = (toward - transform.position).normalized,
                Distance = dashData.dashDistance,
                Speed = dashData.dashSpeed
            };

            MovementInput input = CreateDashInput(dashInputData);
            SendInputToServer(input);
        }
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

        if (serverAgent != null && _baseMaxSpeed > 0f)
        {
            serverAgent.speed = _baseMaxSpeed;
        }
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

    #region Private Methods

    private void ConfigureNetworkRole()
    {
        if (IsOwner)
        {
            serverAgent.enabled = true;
            serverAgent.updatePosition = true;
            serverAgent.updateRotation = true;
        }
        else if (IsServer)
        {
            serverAgent.enabled = true;
            serverAgent.updatePosition = true;
            serverAgent.updateRotation = true;
        }
        else
        {
            serverAgent.enabled = false;
        }
    }

    private float GetNetworkTime()
    {
        if (NetworkManager.Singleton != null)
        {
            return (float)NetworkManager.Singleton.ServerTime.Time;
        }
        return Time.time;
    }

    private MovementInput CreateClickToMoveInput(Vector3 targetPosition)
    {
        MovementInput input = MovementInput.CreateClickToMove(
            _nextSequenceNumber++,
            GetNetworkTime(),
            targetPosition
        );

        AddPendingInput(input);
        return input;
    }

    private MovementInput CreateStopInput()
    {
        MovementInput input = MovementInput.CreateStop(
            _nextSequenceNumber++,
            GetNetworkTime()
        );

        AddPendingInput(input);
        return input;
    }

    private MovementInput CreateDashInput(DashInputData dashData)
    {
        MovementInput input = MovementInput.CreateDash(
            _nextSequenceNumber++,
            GetNetworkTime(),
            dashData
        );

        AddPendingInput(input);
        return input;
    }

    private void AddPendingInput(MovementInput input)
    {
        while (_pendingInputs.Count >= MAX_PENDING_INPUTS)
        {
            _pendingInputs.Dequeue();
        }
        _pendingInputs.Enqueue(input);
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

        _lastAcknowledgedSequence = input.SequenceNumber;
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
        float currentTime = GetNetworkTime();

        _positionHistory.RecordPosition(
            currentTime,
            transform.position,
            transform.rotation,
            CalculateHitboxBounds()
        );

        if (currentTime - _lastStateBroadcastTime >= stateBroadcastInterval)
        {
            BroadcastState();
            _lastStateBroadcastTime = currentTime;
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

    private void BroadcastState()
    {
        MovementState state = MovementState.Create(
            _lastAcknowledgedSequence,
            GetNetworkTime(),
            transform.position,
            serverAgent.velocity,
            transform.rotation,
            IsMoving,
            _canMove,
            _isDashing,
            serverAgent.destination
        );

        BroadcastMovementStateClientRpc(state);
        _lastBroadcastPosition = transform.position;
    }

    [Rpc(SendTo.Everyone, Delivery = RpcDelivery.Unreliable)]
    private void BroadcastMovementStateClientRpc(MovementState state)
    {
        OnServerStateReceived(state);
    }

    private void ReconcileWithServerState(MovementState state)
    {
        RemoveAcknowledgedInputs(state.LastProcessedSequence);

        if (!state.CanMove && _canMove)
        {
            _pendingInputs.Clear();
            StopMovement();
        }
        _canMove = state.CanMove;

        if (_isDashing)
        {
            return;
        }

        Vector3 predictedPosition = transform.position;
        Vector3 serverPosition = state.Position;
        Vector3 error = serverPosition - predictedPosition;
        float errorMagnitude = error.magnitude;

        if (errorMagnitude > snapThreshold)
        {
            transform.position = serverPosition;
            _correctionOffset = Vector3.zero;
            _consecutiveLargeCorrections = 0;
        }
        else if (errorMagnitude > 0.01f)
        {
            if (errorMagnitude > LARGE_ERROR_THRESHOLD)
            {
                _consecutiveLargeCorrections++;

                if (_consecutiveLargeCorrections > MAX_CONSECUTIVE_LARGE_CORRECTIONS)
                {
                    transform.position = serverPosition;
                    _correctionOffset = Vector3.zero;
                    _pendingInputs.Clear();
                    _consecutiveLargeCorrections = 0;
                    return;
                }
            }
            else
            {
                _consecutiveLargeCorrections = 0;
            }

            Vector3 previousVisualPosition = transform.position + _correctionOffset;
            transform.position = serverPosition;
            ReplayPendingInputs();

            _correctionOffset = previousVisualPosition - transform.position;
            UpdateVisualPositionImmediate();
        }
    }

    private void RemoveAcknowledgedInputs(uint lastProcessedSequence)
    {
        while (_pendingInputs.Count > 0)
        {
            MovementInput oldest = _pendingInputs.Peek();
            if (IsSequenceNewer(lastProcessedSequence, oldest.SequenceNumber) ||
                oldest.SequenceNumber == lastProcessedSequence)
            {
                _pendingInputs.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    private bool IsSequenceNewer(uint a, uint b)
    {
        return (a > b) && (a - b < uint.MaxValue / 2) ||
               (b > a) && (b - a > uint.MaxValue / 2);
    }

    private void ReplayPendingInputs()
    {
        foreach (MovementInput input in _pendingInputs)
        {
            ReplayInput(input);
        }
    }

    private void ReplayInput(MovementInput input)
    {
        switch (input.InputType)
        {
            case MovementInputType.ClickToMove:
                if (serverAgent.enabled && _canMove)
                {
                    serverAgent.SetDestination(input.TargetPosition);
                }
                break;

            case MovementInputType.Directional:
                if (serverAgent.enabled && _canMove)
                {
                    serverAgent.Move(input.MoveDirection * serverAgent.speed * Time.fixedDeltaTime);
                }
                break;

            case MovementInputType.Dash:
                Vector3 dashEnd = input.DashData.StartPosition +
                                 input.DashData.Direction * input.DashData.Distance;
                transform.position = dashEnd;
                break;

            case MovementInputType.Stop:
                if (serverAgent.enabled)
                {
                    serverAgent.ResetPath();
                    serverAgent.velocity = Vector3.zero;
                }
                break;
        }
    }

    private void UpdateOwnerVisual()
    {
        _correctionOffset = Vector3.Lerp(
            _correctionOffset,
            Vector3.zero,
            correctionSmoothingRate * Time.deltaTime
        );

        UpdateVisualPositionImmediate();
    }

    private void UpdateVisualPositionImmediate()
    {
        if (visualTransform != null)
        {
            visualTransform.position = transform.position + _correctionOffset;
            visualTransform.rotation = transform.rotation;
        }
    }

    private void UpdateNonOwnerInterpolation()
    {
        float networkTime = GetNetworkTime();
        PositionSnapshot snapshot = _interpolationBuffer.GetInterpolatedSnapshot(networkTime);

        if (snapshot.Timestamp > 0f)
        {
            transform.position = snapshot.Position;
            transform.rotation = snapshot.Rotation;

            if (visualTransform != null)
            {
                visualTransform.position = snapshot.Position;
                visualTransform.rotation = snapshot.Rotation;
            }
        }

        _interpolationBuffer.PruneOldSnapshots(networkTime);
    }

    private void UpdateAnimationFromMovement()
    {
        if (animationHandler != null)
        {
            Vector3 velocity = serverAgent != null && serverAgent.enabled
                ? serverAgent.velocity
                : _lastReceivedState.Velocity;

            animationHandler.UpdateMovement(velocity);
        }
    }

    private List<Vector3> ComputeDashPositions(DashData dashData, Vector3 toward)
    {
        toward.y = transform.position.y;

        Vector3 direction = toward - transform.position;
        direction = direction.normalized;

        Vector3 wantedDestination = transform.position + direction * dashData.dashDistance;

        List<Vector3> dashPositions = CheckWalls(wantedDestination, dashData);

        for (int i = 0; i < dashPositions.Count; i++)
        {
            DebugDrawer.DrawSphere(dashPositions[i], 0.1f, Color.green, 1f);
        }

        return dashPositions;
    }

    private List<Vector3> CheckWalls(Vector3 wantedPosition, DashData dashData)
    {
        List<Vector3> dashPositions = new List<Vector3> { transform.position };
        Vector3 wantedDirection = wantedPosition - dashPositions[^1];

        Physics.Raycast(transform.position, Vector3.down, out RaycastHit down, 2f, groundLayerMask);
        Vector3 groundNormal = down.normal;
        Debug.DrawRay(transform.position, groundNormal, Color.red, 1f);

        wantedDirection = Vector3.ProjectOnPlane(wantedDirection, groundNormal);
        wantedDirection = wantedDirection.normalized;

        Vector3 remaining = wantedPosition - dashPositions[^1];

        bool forwardCheck = serverAgent.Raycast(wantedPosition, out NavMeshHit forwardHit);
        int iterationCount = 0;
        int maxIterations = dashData.maxWallIterations > 0
            ? dashData.maxWallIterations
            : DEFAULT_MAX_WALL_ITERATIONS;

        while (forwardCheck && iterationCount < maxIterations)
        {
            iterationCount++;
            Debug.DrawRay(forwardHit.position, forwardHit.normal * 2, Color.red, 3f);
            DebugDrawer.DrawSphere(forwardHit.position, AgentRadius, Color.cyan, 3f);

            dashPositions.Add(forwardHit.position);

            if (!dashData.slideOnWalls)
            {
                remaining = Vector3.zero;
                break;
            }

            remaining = wantedPosition - dashPositions[^1];
            remaining = Vector3.ProjectOnPlane(remaining, forwardHit.normal);
            wantedPosition = remaining + dashPositions[^1];

            wantedDirection = wantedPosition - dashPositions[^1];
            wantedDirection = wantedDirection.normalized;

            forwardCheck = NavMesh.Raycast(serverAgent.nextPosition, wantedPosition, out forwardHit, NavMesh.AllAreas);
        }

        if (forwardCheck && iterationCount >= maxIterations)
        {
            LogWallCheckLimitReached(this, maxIterations);
            remaining = Vector3.zero;
        }

        dashPositions.Add(dashPositions[^1] + wantedDirection * remaining.magnitude);

        return dashPositions;
    }

    internal static void LogWallCheckLimitReached(Component owner, int maxIterations)
    {
        string ownerName = owner != null ? owner.name : "(unknown object)";
        Debug.LogWarning($"Dash wall check reached the max iteration count ({maxIterations}) for {ownerName}. Ending dash early to avoid infinite loop.");
    }

    private IEnumerator DashRoutine(List<Vector3> dashPosition, DashData dashData)
    {
        SetBodyDashRotation(dashPosition);

        float dashDuration = dashData.dashDistance / dashData.dashSpeed;
        float elapsedTime = 0;
        float normalizedTime;
        float curvePosition;
        Vector3 nextPosition;

        while (elapsedTime < dashDuration)
        {
            elapsedTime += Time.deltaTime;
            normalizedTime = Mathf.Clamp01(elapsedTime / dashDuration);

            curvePosition = dashData.dashCurve.Evaluate(normalizedTime);
            nextPosition = InterpolatePath(dashPosition, curvePosition);
            serverAgent.nextPosition = nextPosition;

            yield return null;
        }

        serverAgent.enabled = true;
        _canMove = true;
        _isDashing = false;
    }

    private void SetBodyDashRotation(List<Vector3> dashPosition)
    {
        Vector3 startPos = transform.position;
        Vector3 nextPos = dashPosition[0];

        Vector3 lookAt = nextPos - startPos;
        lookAt.y = 0f;
        Quaternion lookRot = Vector3.SqrMagnitude(lookAt) == 0
            ? Quaternion.LookRotation(transform.forward)
            : Quaternion.LookRotation(lookAt);
        body.rotation = lookRot;
    }

    private Vector3 InterpolatePath(List<Vector3> path, float t)
    {
        float segmentCount = path.Count - 1;
        float segmentIndex = t * segmentCount;
        int currentSegment = Mathf.FloorToInt(segmentIndex);
        int nextSegment = Mathf.Clamp(currentSegment + 1, 0, path.Count - 1);

        float segmentFraction = segmentIndex - currentSegment;

        return Vector3.Lerp(path[currentSegment], path[nextSegment], segmentFraction);
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

    private void SubscribeToModifierChanges()
    {
        if (modifierManager != null)
        {
            modifierManager.OnGlobalModifiersChanged -= RefreshMovementSpeedFromStats;
            modifierManager.OnGlobalModifiersChanged += RefreshMovementSpeedFromStats;
            modifierManager.OnAnyModifiersChanged -= RefreshMovementSpeedFromStats;
            modifierManager.OnAnyModifiersChanged += RefreshMovementSpeedFromStats;
        }
    }

    private void UnsubscribeFromModifierChanges()
    {
        if (modifierManager != null)
        {
            modifierManager.OnGlobalModifiersChanged -= RefreshMovementSpeedFromStats;
            modifierManager.OnAnyModifiersChanged -= RefreshMovementSpeedFromStats;
        }
    }

    #endregion
}
