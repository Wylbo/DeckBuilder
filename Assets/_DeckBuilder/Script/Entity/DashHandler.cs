using System.Collections.Generic;
using MG.Extend;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles dash path computation and wall collision detection.
/// Computes dash positions with wall sliding and path interpolation.
/// </summary>
public class DashHandler
{
    #region Fields

    /// <summary>Default maximum iterations for wall checking to prevent infinite loops.</summary>
    internal const int DEFAULT_MAX_WALL_ITERATIONS = 100;

    #endregion

    #region Public Methods

    /// <summary>
    /// Computes the dash path positions including wall collision handling.
    /// </summary>
    /// <param name="dashData">The dash configuration data.</param>
    /// <param name="startPosition">The starting position of the dash.</param>
    /// <param name="toward">The target position to dash toward.</param>
    /// <param name="agent">The NavMeshAgent for pathfinding queries.</param>
    /// <param name="groundLayerMask">Layer mask for ground detection.</param>
    /// <param name="agentRadius">The agent's radius for collision visualization.</param>
    /// <returns>A list of positions defining the dash path.</returns>
    public List<Vector3> ComputeDashPositions(
        Movement.DashData dashData,
        Vector3 startPosition,
        Vector3 toward,
        NavMeshAgent agent,
        LayerMask groundLayerMask,
        float agentRadius)
    {
        toward.y = startPosition.y;

        Vector3 direction = toward - startPosition;
        direction = direction.normalized;

        Vector3 wantedDestination = startPosition + direction * dashData.dashDistance;

        List<Vector3> dashPositions = CheckWalls(
            startPosition,
            wantedDestination,
            dashData,
            agent,
            groundLayerMask,
            agentRadius
        );

        DrawDebugPositions(dashPositions);

        return dashPositions;
    }

    /// <summary>
    /// Interpolates along a path based on a normalized time value.
    /// </summary>
    /// <param name="path">The path positions to interpolate along.</param>
    /// <param name="t">The normalized time (0 to 1) along the path.</param>
    /// <returns>The interpolated position.</returns>
    public Vector3 InterpolatePath(List<Vector3> path, float t)
    {
        if (path == null || path.Count == 0)
        {
            return Vector3.zero;
        }

        if (path.Count == 1)
        {
            return path[0];
        }

        float segmentCount = path.Count - 1;
        float segmentIndex = t * segmentCount;
        int currentSegment = Mathf.FloorToInt(segmentIndex);
        currentSegment = Mathf.Clamp(currentSegment, 0, path.Count - 2);
        int nextSegment = currentSegment + 1;

        float segmentFraction = segmentIndex - currentSegment;

        return Vector3.Lerp(path[currentSegment], path[nextSegment], segmentFraction);
    }

    /// <summary>
    /// Calculates the rotation for the entity during a dash.
    /// </summary>
    /// <param name="startPosition">The starting position.</param>
    /// <param name="targetPosition">The first target position in the dash path.</param>
    /// <param name="fallbackForward">The fallback forward direction if no valid direction can be computed.</param>
    /// <returns>The rotation facing the dash direction.</returns>
    public Quaternion CalculateDashRotation(Vector3 startPosition, Vector3 targetPosition, Vector3 fallbackForward)
    {
        Vector3 lookAt = targetPosition - startPosition;
        lookAt.y = 0f;

        if (Vector3.SqrMagnitude(lookAt) == 0)
        {
            return Quaternion.LookRotation(fallbackForward);
        }

        return Quaternion.LookRotation(lookAt);
    }

    /// <summary>
    /// Calculates the dash duration based on distance and speed.
    /// </summary>
    /// <param name="dashDistance">The total dash distance.</param>
    /// <param name="dashSpeed">The dash speed.</param>
    /// <returns>The duration of the dash in seconds.</returns>
    public float CalculateDashDuration(float dashDistance, float dashSpeed)
    {
        if (dashSpeed <= 0f)
        {
            return 0f;
        }

        return dashDistance / dashSpeed;
    }

    #endregion

    #region Private Methods

    private List<Vector3> CheckWalls(
        Vector3 startPosition,
        Vector3 wantedPosition,
        Movement.DashData dashData,
        NavMeshAgent agent,
        LayerMask groundLayerMask,
        float agentRadius)
    {
        List<Vector3> dashPositions = new List<Vector3> { startPosition };
        Vector3 wantedDirection = wantedPosition - dashPositions[dashPositions.Count - 1];

        Vector3 groundNormal = GetGroundNormal(startPosition, groundLayerMask);
        Debug.DrawRay(startPosition, groundNormal, Color.red, 1f);

        wantedDirection = Vector3.ProjectOnPlane(wantedDirection, groundNormal);
        wantedDirection = wantedDirection.normalized;

        Vector3 remaining = wantedPosition - dashPositions[dashPositions.Count - 1];

        bool forwardCheck = agent.Raycast(wantedPosition, out NavMeshHit forwardHit);
        int iterationCount = 0;
        int maxIterations = GetMaxIterations(dashData);

        while (forwardCheck && iterationCount < maxIterations)
        {
            iterationCount++;
            DrawWallHitDebug(forwardHit, agentRadius);

            dashPositions.Add(forwardHit.position);

            if (!dashData.slideOnWalls)
            {
                remaining = Vector3.zero;
                break;
            }

            remaining = wantedPosition - dashPositions[dashPositions.Count - 1];
            remaining = Vector3.ProjectOnPlane(remaining, forwardHit.normal);
            wantedPosition = remaining + dashPositions[dashPositions.Count - 1];

            wantedDirection = wantedPosition - dashPositions[dashPositions.Count - 1];
            wantedDirection = wantedDirection.normalized;

            forwardCheck = NavMesh.Raycast(agent.nextPosition, wantedPosition, out forwardHit, NavMesh.AllAreas);
        }

        if (forwardCheck && iterationCount >= maxIterations)
        {
            LogWallCheckLimitReached(maxIterations);
            remaining = Vector3.zero;
        }

        Vector3 finalPosition = dashPositions[dashPositions.Count - 1] + wantedDirection * remaining.magnitude;
        dashPositions.Add(finalPosition);

        return dashPositions;
    }

    private Vector3 GetGroundNormal(Vector3 position, LayerMask groundLayerMask)
    {
        if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, 2f, groundLayerMask))
        {
            return hit.normal;
        }

        return Vector3.up;
    }

    private int GetMaxIterations(Movement.DashData dashData)
    {
        return dashData.maxWallIterations > 0
            ? dashData.maxWallIterations
            : DEFAULT_MAX_WALL_ITERATIONS;
    }

    private void DrawWallHitDebug(NavMeshHit hit, float agentRadius)
    {
        Debug.DrawRay(hit.position, hit.normal * 2, Color.red, 3f);
        DebugDrawer.DrawSphere(hit.position, agentRadius, Color.cyan, 3f);
    }

    private void DrawDebugPositions(List<Vector3> dashPositions)
    {
        for (int i = 0; i < dashPositions.Count; i++)
        {
            DebugDrawer.DrawSphere(dashPositions[i], 0.1f, Color.green, 1f);
        }
    }

    private void LogWallCheckLimitReached(int maxIterations)
    {
        Debug.LogWarning($"Dash wall check reached the max iteration count ({maxIterations}). Ending dash early to avoid infinite loop.");
    }

    #endregion
}
