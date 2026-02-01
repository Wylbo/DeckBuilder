/// <summary>
/// Interface for replaying movement inputs during client-side prediction reconciliation.
/// Abstracts the replay mechanism from the prediction handler.
/// </summary>
public interface IInputReplayer
{
    /// <summary>
    /// Replays a click-to-move input by setting the NavMeshAgent destination.
    /// </summary>
    /// <param name="targetPosition">The world position to move toward.</param>
    void ReplayClickToMove(UnityEngine.Vector3 targetPosition);

    /// <summary>
    /// Replays a directional movement input.
    /// </summary>
    /// <param name="direction">The normalized movement direction.</param>
    /// <param name="speed">The movement speed.</param>
    /// <param name="deltaTime">The time delta for this movement step.</param>
    void ReplayDirectional(UnityEngine.Vector3 direction, float speed, float deltaTime);

    /// <summary>
    /// Replays a dash input by teleporting to the dash end position.
    /// </summary>
    /// <param name="dashData">The dash input data containing start position, direction, and distance.</param>
    void ReplayDash(DashInputData dashData);

    /// <summary>
    /// Replays a stop input by resetting the path and velocity.
    /// </summary>
    void ReplayStop();
}
