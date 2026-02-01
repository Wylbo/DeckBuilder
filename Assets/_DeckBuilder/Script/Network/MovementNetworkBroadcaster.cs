using UnityEngine;

/// <summary>
/// Manages server state broadcast timing and creation.
/// Responsible for determining when to broadcast and creating movement state snapshots.
/// </summary>
public class MovementNetworkBroadcaster
{
    #region Private Members

    private float _lastBroadcastTime;
    private Vector3 _lastBroadcastPosition;

    #endregion

    #region Getters

    /// <summary>Gets the time of the last state broadcast.</summary>
    public float LastBroadcastTime => _lastBroadcastTime;

    /// <summary>Gets the position from the last broadcast.</summary>
    public Vector3 LastBroadcastPosition => _lastBroadcastPosition;

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines if a state broadcast should occur based on the interval.
    /// </summary>
    /// <param name="currentTime">The current network time.</param>
    /// <param name="broadcastInterval">The minimum interval between broadcasts.</param>
    /// <returns>True if enough time has passed for a broadcast; otherwise false.</returns>
    public bool ShouldBroadcast(float currentTime, float broadcastInterval)
    {
        return currentTime - _lastBroadcastTime >= broadcastInterval;
    }

    /// <summary>
    /// Creates a movement state snapshot for broadcasting.
    /// </summary>
    /// <param name="lastAcknowledgedSequence">The last input sequence number processed.</param>
    /// <param name="serverTime">The current server time.</param>
    /// <param name="position">The current position.</param>
    /// <param name="velocity">The current velocity.</param>
    /// <param name="rotation">The current rotation.</param>
    /// <param name="isMoving">Whether currently moving.</param>
    /// <param name="canMove">Whether movement is allowed.</param>
    /// <param name="isDashing">Whether currently dashing.</param>
    /// <param name="destination">The current destination.</param>
    /// <returns>The created movement state.</returns>
    public MovementState CreateMovementState(
        uint lastAcknowledgedSequence,
        float serverTime,
        Vector3 position,
        Vector3 velocity,
        Quaternion rotation,
        bool isMoving,
        bool canMove,
        bool isDashing,
        Vector3 destination)
    {
        return MovementState.Create(
            lastAcknowledgedSequence,
            serverTime,
            position,
            velocity,
            rotation,
            isMoving,
            canMove,
            isDashing,
            destination
        );
    }

    /// <summary>
    /// Records that a broadcast occurred at the specified time and position.
    /// </summary>
    /// <param name="currentTime">The time of the broadcast.</param>
    /// <param name="position">The position that was broadcast.</param>
    public void RecordBroadcast(float currentTime, Vector3 position)
    {
        _lastBroadcastTime = currentTime;
        _lastBroadcastPosition = position;
    }

    /// <summary>
    /// Resets the broadcaster to its initial state.
    /// </summary>
    public void Reset()
    {
        _lastBroadcastTime = 0f;
        _lastBroadcastPosition = Vector3.zero;
    }

    #endregion
}
