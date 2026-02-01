using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Authoritative movement state broadcast from server to clients.
/// Contains the server's validated position and movement data for reconciliation.
/// </summary>
public struct MovementState : INetworkSerializable
{
    #region Fields

    /// <summary>The sequence number of the last input processed by the server.</summary>
    public uint LastProcessedSequence;

    /// <summary>Server-synchronized timestamp when this state was generated.</summary>
    public float ServerTimestamp;

    /// <summary>Authoritative world position.</summary>
    public Vector3 Position;

    /// <summary>Current velocity of the entity.</summary>
    public Vector3 Velocity;

    /// <summary>Authoritative rotation.</summary>
    public Quaternion Rotation;

    /// <summary>Whether the entity is currently moving.</summary>
    public bool IsMoving;

    /// <summary>Whether the entity is allowed to move.</summary>
    public bool CanMove;

    /// <summary>Whether the entity is currently dashing.</summary>
    public bool IsDashing;

    /// <summary>The current movement destination, if any.</summary>
    public Vector3 Destination;

    #endregion

    #region Getters

    /// <summary>
    /// Gets a value indicating whether this state has a valid destination.
    /// </summary>
    public bool HasDestination => IsMoving && Destination != Vector3.zero;

    #endregion

    #region Public Methods

    /// <summary>
    /// Serializes the movement state for network transmission.
    /// </summary>
    /// <typeparam name="T">The buffer serializer type.</typeparam>
    /// <param name="serializer">The serializer to use for reading/writing.</param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref LastProcessedSequence);
        serializer.SerializeValue(ref ServerTimestamp);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref Rotation);
        serializer.SerializeValue(ref IsMoving);
        serializer.SerializeValue(ref CanMove);
        serializer.SerializeValue(ref IsDashing);
        serializer.SerializeValue(ref Destination);
    }

    /// <summary>
    /// Creates a default idle state at the specified position.
    /// </summary>
    /// <param name="position">The position of the entity.</param>
    /// <param name="rotation">The rotation of the entity.</param>
    /// <returns>A configured MovementState for an idle entity.</returns>
    public static MovementState CreateIdle(Vector3 position, Quaternion rotation)
    {
        return new MovementState
        {
            LastProcessedSequence = 0,
            ServerTimestamp = 0f,
            Position = position,
            Velocity = Vector3.zero,
            Rotation = rotation,
            IsMoving = false,
            CanMove = true,
            IsDashing = false,
            Destination = Vector3.zero
        };
    }

    /// <summary>
    /// Creates a state from current movement data.
    /// </summary>
    /// <param name="lastProcessedSequence">The last input sequence number processed.</param>
    /// <param name="serverTimestamp">The current server timestamp.</param>
    /// <param name="position">Current position.</param>
    /// <param name="velocity">Current velocity.</param>
    /// <param name="rotation">Current rotation.</param>
    /// <param name="isMoving">Whether currently moving.</param>
    /// <param name="canMove">Whether movement is allowed.</param>
    /// <param name="isDashing">Whether currently dashing.</param>
    /// <param name="destination">Current destination.</param>
    /// <returns>A configured MovementState.</returns>
    public static MovementState Create(
        uint lastProcessedSequence,
        float serverTimestamp,
        Vector3 position,
        Vector3 velocity,
        Quaternion rotation,
        bool isMoving,
        bool canMove,
        bool isDashing,
        Vector3 destination)
    {
        return new MovementState
        {
            LastProcessedSequence = lastProcessedSequence,
            ServerTimestamp = serverTimestamp,
            Position = position,
            Velocity = velocity,
            Rotation = rotation,
            IsMoving = isMoving,
            CanMove = canMove,
            IsDashing = isDashing,
            Destination = destination
        };
    }

    /// <summary>
    /// Calculates the position error between this state and a predicted position.
    /// </summary>
    /// <param name="predictedPosition">The client's predicted position.</param>
    /// <returns>The error vector from predicted to authoritative position.</returns>
    public Vector3 CalculatePositionError(Vector3 predictedPosition)
    {
        return Position - predictedPosition;
    }

    /// <summary>
    /// Determines if the position error exceeds the snap threshold.
    /// </summary>
    /// <param name="predictedPosition">The client's predicted position.</param>
    /// <param name="snapThreshold">The threshold beyond which to snap instead of interpolate.</param>
    /// <returns>True if the error exceeds the threshold; otherwise false.</returns>
    public bool ShouldSnapToPosition(Vector3 predictedPosition, float snapThreshold)
    {
        return CalculatePositionError(predictedPosition).sqrMagnitude > snapThreshold * snapThreshold;
    }

    #endregion
}
