using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Defines the type of movement input being sent from client to server.
/// </summary>
public enum MovementInputType : byte
{
    /// <summary>No movement input.</summary>
    None = 0,

    /// <summary>Click-to-move navigation to a world position.</summary>
    ClickToMove = 1,

    /// <summary>Directional movement input (WASD/joystick).</summary>
    Directional = 2,

    /// <summary>Dash ability movement.</summary>
    Dash = 3,

    /// <summary>Stop all movement.</summary>
    Stop = 4
}

/// <summary>
/// Contains data specific to dash movement inputs.
/// Used for server validation and replay during reconciliation.
/// </summary>
public struct DashInputData : INetworkSerializable
{
    #region Fields

    /// <summary>Position where the dash started, used for server validation.</summary>
    public Vector3 StartPosition;

    /// <summary>Direction of the dash.</summary>
    public Vector3 Direction;

    /// <summary>Distance to travel during the dash.</summary>
    public float Distance;

    /// <summary>Speed of the dash movement.</summary>
    public float Speed;

    #endregion

    #region Public Methods

    /// <summary>
    /// Serializes the dash input data for network transmission.
    /// </summary>
    /// <typeparam name="T">The buffer serializer type.</typeparam>
    /// <param name="serializer">The serializer to use for reading/writing.</param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref StartPosition);
        serializer.SerializeValue(ref Direction);
        serializer.SerializeValue(ref Distance);
        serializer.SerializeValue(ref Speed);
    }

    #endregion
}

/// <summary>
/// Represents a single movement input sent from client to server.
/// Tagged with a sequence number for tracking acknowledgment and reconciliation.
/// </summary>
public struct MovementInput : INetworkSerializable
{
    #region Fields

    /// <summary>Unique sequence number for this input, used for acknowledgment tracking.</summary>
    public uint SequenceNumber;

    /// <summary>Network-synchronized timestamp when this input was created.</summary>
    public float Timestamp;

    /// <summary>Target world position for click-to-move inputs.</summary>
    public Vector3 TargetPosition;

    /// <summary>Movement direction for directional inputs.</summary>
    public Vector3 MoveDirection;

    /// <summary>The type of movement input.</summary>
    public MovementInputType InputType;

    /// <summary>Additional data for dash inputs.</summary>
    public DashInputData DashData;

    #endregion

    #region Public Methods

    /// <summary>
    /// Serializes the movement input for network transmission.
    /// </summary>
    /// <typeparam name="T">The buffer serializer type.</typeparam>
    /// <param name="serializer">The serializer to use for reading/writing.</param>
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SequenceNumber);
        serializer.SerializeValue(ref Timestamp);
        serializer.SerializeValue(ref TargetPosition);
        serializer.SerializeValue(ref MoveDirection);
        serializer.SerializeValue(ref InputType);
        serializer.SerializeValue(ref DashData);
    }

    /// <summary>
    /// Creates a click-to-move input.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number for this input.</param>
    /// <param name="timestamp">The network timestamp.</param>
    /// <param name="targetPosition">The target world position to move to.</param>
    /// <returns>A configured MovementInput for click-to-move.</returns>
    public static MovementInput CreateClickToMove(uint sequenceNumber, float timestamp, Vector3 targetPosition)
    {
        return new MovementInput
        {
            SequenceNumber = sequenceNumber,
            Timestamp = timestamp,
            TargetPosition = targetPosition,
            MoveDirection = Vector3.zero,
            InputType = MovementInputType.ClickToMove,
            DashData = default
        };
    }

    /// <summary>
    /// Creates a directional movement input.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number for this input.</param>
    /// <param name="timestamp">The network timestamp.</param>
    /// <param name="direction">The movement direction.</param>
    /// <returns>A configured MovementInput for directional movement.</returns>
    public static MovementInput CreateDirectional(uint sequenceNumber, float timestamp, Vector3 direction)
    {
        return new MovementInput
        {
            SequenceNumber = sequenceNumber,
            Timestamp = timestamp,
            TargetPosition = Vector3.zero,
            MoveDirection = direction.normalized,
            InputType = MovementInputType.Directional,
            DashData = default
        };
    }

    /// <summary>
    /// Creates a dash movement input.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number for this input.</param>
    /// <param name="timestamp">The network timestamp.</param>
    /// <param name="dashData">The dash-specific data.</param>
    /// <returns>A configured MovementInput for dash movement.</returns>
    public static MovementInput CreateDash(uint sequenceNumber, float timestamp, DashInputData dashData)
    {
        return new MovementInput
        {
            SequenceNumber = sequenceNumber,
            Timestamp = timestamp,
            TargetPosition = Vector3.zero,
            MoveDirection = Vector3.zero,
            InputType = MovementInputType.Dash,
            DashData = dashData
        };
    }

    /// <summary>
    /// Creates a stop movement input.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number for this input.</param>
    /// <param name="timestamp">The network timestamp.</param>
    /// <returns>A configured MovementInput for stopping.</returns>
    public static MovementInput CreateStop(uint sequenceNumber, float timestamp)
    {
        return new MovementInput
        {
            SequenceNumber = sequenceNumber,
            Timestamp = timestamp,
            TargetPosition = Vector3.zero,
            MoveDirection = Vector3.zero,
            InputType = MovementInputType.Stop,
            DashData = default
        };
    }

    #endregion
}
