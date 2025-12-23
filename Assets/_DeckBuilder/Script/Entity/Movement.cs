using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MG.Extend;
using Unity.Netcode;


/// <summary>
/// Component allowing an entity to move
/// </summary>

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(GlobalStatSource))]
[RequireComponent(typeof(StatsModifierManager))]
[RequireComponent(typeof(AnimationHandler))]
public class Movement : NetworkBehaviour, IAbilityMovement
{
    [Serializable]
    public struct DashData
    {
        [SerializeField]
        public float dashDistance;
        [SerializeField]
        public float dashSpeed;
        [SerializeField]
        public bool slideOnWalls;
        [SerializeField]
        public AnimationCurve dashCurve;
        [SerializeField]
        public LayerMask blockingMask;
        [SerializeField]
        [Min(1)]
        public int maxWallIterations;
    }

    [SerializeField] private GlobalStatKey movementSpeedStatKey = GlobalStatKey.MovementSpeed;
    [SerializeField] private GlobalStatSource globalStatSource = null;
    [SerializeField] private StatsModifierManager modifierManager = null;
    [SerializeField]
    private Rigidbody body = null;

    [SerializeField]
    private NavMeshAgent serverAgent = null;
    [SerializeField]
    private CapsuleCollider capsuleCollider = null;
    [SerializeField]
    private LayerMask groundLayerMask;
    [SerializeField]
    private DashData defaultDashData = new DashData();

    [Header("Animation")]
    [SerializeField] private AnimationHandler animationHandler = null;

    [Header("Movement prediction")]

    private bool canMove = true;
    private Coroutine dashRoutine;
    private float baseMaxSpeed = 0;

    // Netcode General
    private NetworkTimer networkTimer;
    const float SERVER_TICK_RATE = 60f;
    const int BUFFER_SIZE = 1024;

    // Netcode client specific
    private CircularBuffer<StatePayload> _clientStateBuffer;
    private CircularBuffer<InputPayload> _clientInputBuffer;
    private StatePayload _lastServerState;
    private InputPayload _lastProcessedInput;

    // Netcode server specific
    private CircularBuffer<StatePayload> _serverStateBuffer;
    private Queue<InputPayload> _serverInputQueue;

    public NavMeshAgent Agent => serverAgent;
    private float AgentRadius => NavMesh.GetSettingsByID(serverAgent.agentTypeID).agentRadius;
    private float StepHeight => NavMesh.GetSettingsByID(serverAgent.agentTypeID).agentClimb;
    private float HalfHeight => capsuleCollider.height / 2;
    public bool CanMove => canMove;
    public bool IsMoving => serverAgent.velocity.magnitude > 0;
    public Rigidbody Body => body;

    internal const int DefaultMaxWallIterations = 100;

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
        if (serverAgent == null)
            serverAgent = GetComponent<NavMeshAgent>();
        if (globalStatSource == null)
            globalStatSource = GetComponent<GlobalStatSource>();
        if (modifierManager == null)
            modifierManager = GetComponent<StatsModifierManager>();
        if (animationHandler == null)
            animationHandler = GetComponent<AnimationHandler>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // bool isServer = IsServer;
        // serverAgent.enabled = isServer;

        // Ensure the visual agent is only active for the owner on clients.
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    private void Awake()
    {
        baseMaxSpeed = serverAgent != null ? serverAgent.speed : 0f;
        RefreshMovementSpeedFromStats();

        networkTimer = new NetworkTimer(SERVER_TICK_RATE);
        _clientStateBuffer = new CircularBuffer<StatePayload>(BUFFER_SIZE);
        _clientInputBuffer = new CircularBuffer<InputPayload>(BUFFER_SIZE);
        _serverStateBuffer = new CircularBuffer<StatePayload>(BUFFER_SIZE);
        _serverInputQueue = new Queue<InputPayload>();
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
            defaultDashData.maxWallIterations = DefaultMaxWallIterations;
        }
    }

    private void Update()
    {
        networkTimer.Update(Time.deltaTime);
        animationHandler?.UpdateMovement(serverAgent.velocity);
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        while (networkTimer.ShouldTick())
        {
            // Process movement prediction here
            HandleClientTick();
            HandleServerTick();
        }
    }

    private void HandleClientTick()
    {
        if (!IsClient)
            return;

        int currentTick = networkTimer.CurrentTick;
        int bufferIndex = currentTick % BUFFER_SIZE;

        InputPayload inputPayload = new InputPayload
        {
            tick = currentTick,
            moveTo = Vector3.zero // Placeholder for actual input
        };

        _clientInputBuffer.Add(inputPayload, bufferIndex);
        SendTo_ServerRpc(inputPayload);

        StatePayload statePayload = ProcessMove(inputPayload);
        _clientStateBuffer.Add(statePayload, bufferIndex);

        //HandleServerReconciliation();
    }

    private void HandleServerTick()
    {
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SendTo_ServerRpc(InputPayload inputPayload)
    {

    }

    private StatePayload ProcessMove(InputPayload inputPayload)
    {
        MoveTo(inputPayload.moveTo);
        return new StatePayload
        {
            tick = networkTimer.CurrentTick,
            position = serverAgent.transform.position,
            rotation = serverAgent.transform.rotation,
            velocity = serverAgent.velocity,
            angularVelocity = Vector3.zero // NavMeshAgent does not provide angular velocity
        };
    }

    public bool MoveTo(Vector3 worldTo)
    {
        if (!IsOwner)
            return false;

        // predictedNavVisual?.PredictMove(worldTo);
        // RequestMoveTo_ServerRpc(worldTo);
        Move_Internal(worldTo);
        return true;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveTo_ServerRpc(Vector3 worldTo)
    {
        Move_Internal(worldTo);
    }

    public void StopMovement()
    {
        if (!serverAgent.enabled)
            return;

        serverAgent.ResetPath();
        serverAgent.velocity = Vector3.zero;
    }

    public void DisableMovement()
    {
        StopMovement();
        canMove = false;
    }

    public void EnableMovement()
    {
        canMove = true;
    }

    private void Move_Internal(Vector3 worldTo)
    {
        if (!serverAgent.enabled || !CanMove)
            return;

        serverAgent.SetDestination(worldTo);
    }

    #region Dash
    public void Dash()
    {
        Dash(defaultDashData, transform.position + transform.forward);
    }

    public void Dash(Vector3 toward)
    {
        Dash(defaultDashData, toward);
    }

    public void Dash(DashData dashData)
    {
        Dash(dashData, transform.position + transform.forward);
    }

    public void Dash(DashData dashData, Vector3 toward)
    {
        StopMovement();

        if (dashRoutine != null)
            StopCoroutine(dashRoutine);

        List<Vector3> dashPositions = ComputeDashPositions(dashData, toward);

        canMove = false;
        dashRoutine = StartCoroutine(DashRoutine(dashPositions, dashData));
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
        List<Vector3> dashPositions = new List<Vector3>() { transform.position };
        Vector3 wantedDirection = wantedPosition - dashPositions[^1];

        Physics.Raycast(transform.position, Vector3.down, out RaycastHit down, 2f, groundLayerMask);
        Vector3 groundNormal = down.normal;
        Debug.DrawRay(transform.position, groundNormal, Color.red, 1f);

        wantedDirection = Vector3.ProjectOnPlane(wantedDirection, groundNormal);
        wantedDirection = wantedDirection.normalized;

        Vector3 remaining = wantedPosition - dashPositions[^1];

        bool forwardCheck = serverAgent.Raycast(wantedPosition, out NavMeshHit forwardHit);
        int iterationCount = 0;
        int maxIterations = dashData.maxWallIterations > 0 ? dashData.maxWallIterations : DefaultMaxWallIterations;
        while (forwardCheck && iterationCount < maxIterations)
        {
            iterationCount++;
            //Draw normal hit and position
            Debug.DrawRay(forwardHit.position, forwardHit.normal * 2, Color.red, 3f);
            DebugDrawer.DrawSphere(forwardHit.position, AgentRadius, Color.cyan, 3f);

            dashPositions.Add(forwardHit.position);

            if (!dashData.slideOnWalls)
            {
                remaining = Vector3.zero;
                break;
            }

            // calculate the remaining distance on the hitted plane
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
        canMove = true;
    }

    private void SetBodyDashRotation(List<Vector3> dashPosition)
    {
        Vector3 startPos = transform.position;
        Vector3 nextPos = dashPosition[0];

        Vector3 lookAt = nextPos - startPos;
        lookAt.y = 0f;
        Quaternion lookRot = Vector3.SqrMagnitude(lookAt) == 0 ? Quaternion.LookRotation(transform.forward) : Quaternion.LookRotation(lookAt);
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

    /// <summary>
    /// Re-evaluates movement speed using the configured global stat. Call this if global modifiers change.
    /// </summary>
    public void RefreshMovementSpeedFromStats()
    {
        float globalSpeed = GetGlobalMovementSpeed();
        if (globalSpeed > 0f)
            baseMaxSpeed = globalSpeed;

        if (serverAgent != null && baseMaxSpeed > 0f)
            serverAgent.speed = baseMaxSpeed;
    }

    private float GetGlobalMovementSpeed()
    {
        if (globalStatSource == null || serverAgent == null)
            return 0f;

        var stats = globalStatSource.EvaluateGlobalStats();
        if (stats != null && stats.TryGetValue(movementSpeedStatKey, out float modified))
            return modified;

        var raw = globalStatSource.EvaluateGlobalStatsRaw();
        if (raw != null && raw.TryGetValue(movementSpeedStatKey, out float baseVal))
            return baseVal;

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
