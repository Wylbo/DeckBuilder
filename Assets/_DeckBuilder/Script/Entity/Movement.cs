using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MG.Extend;
using Unity.Mathematics;

/// <summary>
/// Component allowing an entity to move
/// </summary>

public class Movement : MonoBehaviour
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
    }

    [SerializeField]
    private Rigidbody body = null;

    [SerializeField]
    private NavMeshAgent agent = null;
    [SerializeField]
    private CapsuleCollider capsuleCollider = null;
    [SerializeField]
    private LayerMask groundLayerMask;

    [SerializeField]
    private DashData defaultDashData = new DashData();
    [SerializeField] private NavMeshQueryFilter navMeshQueryFilter;

    private bool canMove = true;
    private Coroutine dashRoutine;
    private Vector3 wantedVelocity = Vector3.zero;
    private float baseMaxSpeed = 0;

    public NavMeshAgent Agent => agent;
    private float agentRadius => NavMesh.GetSettingsByID(agent.agentTypeID).agentRadius;
    private float stepHeight => NavMesh.GetSettingsByID(agent.agentTypeID).agentClimb;
    private float HalfHeight => capsuleCollider.height / 2;
    public bool CanMove => canMove;
    public bool IsMoving => wantedVelocity.magnitude > 0;
    public Rigidbody Body => body;

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        baseMaxSpeed = agent.speed;
        agent.updateRotation = false;
    }

    private void Update()
    {
        InstantTurn();
        wantedVelocity = agent.velocity;
    }

    public bool MoveTo(Vector3 worldTo)
    {
        if (!agent.enabled || !CanMove)
            return false;

        body.position = agent.nextPosition;
        return agent.SetDestination(worldTo);
    }

    public void StopMovement()
    {
        if (!agent.enabled)
            return;

        agent.ResetPath();
        agent.velocity = Vector3.zero;
        wantedVelocity = Vector3.zero;
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

    public void SpeedChangePercent(float speedChangeRatio)
    {
        agent.speed = baseMaxSpeed * speedChangeRatio;
    }

    public void SpeedChange(float newSpeed)
    {
        agent.speed = newSpeed;
    }

    public void ResetSpeed()
    {
        agent.speed = baseMaxSpeed;
    }

    private void InstantTurn()
    {
        if (!agent.hasPath)
            return;

        Vector3 direction = agent.path.corners[1] - transform.position;
        direction.y = 0;
        direction = direction.normalized;

        Quaternion rotation = quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = rotation;
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

        bool forwardCheck = agent.Raycast(wantedPosition, out NavMeshHit forwardHit);
        int iterationCount = 0;
        while (forwardCheck || iterationCount > 100)
        {
            iterationCount++;
            //Draw normal hit and position
            Debug.DrawRay(forwardHit.position, forwardHit.normal * 2, Color.red, 3f);
            DebugDrawer.DrawSphere(forwardHit.position, agentRadius, Color.cyan, 3f);

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

            forwardCheck = NavMesh.Raycast(agent.nextPosition, wantedPosition, out forwardHit, NavMesh.AllAreas);
        }

        dashPositions.Add(dashPositions[^1] + wantedDirection * remaining.magnitude);

        return dashPositions;
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
            agent.nextPosition = nextPosition;

            yield return null;
        }

        agent.enabled = true;
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
    #endregion
}
