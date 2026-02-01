using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages client-side input prediction and pending input queue.
/// Handles input creation, buffering, acknowledgment, and replay during reconciliation.
/// </summary>
public class MovementPredictionHandler
{
    #region Fields

    /// <summary>Maximum number of pending inputs before oldest are discarded.</summary>
    private const int MAX_PENDING_INPUTS = 64;

    #endregion

    #region Private Members

    private uint _nextSequenceNumber;
    private readonly Queue<MovementInput> _pendingInputs = new Queue<MovementInput>();

    #endregion

    #region Getters

    /// <summary>Gets the current number of pending inputs awaiting server acknowledgment.</summary>
    public int PendingInputCount => _pendingInputs.Count;

    /// <summary>Gets the next sequence number that will be assigned.</summary>
    public uint NextSequenceNumber => _nextSequenceNumber;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a click-to-move input and adds it to the pending queue.
    /// </summary>
    /// <param name="targetPosition">The target world position to move to.</param>
    /// <param name="networkTime">The current network-synchronized time.</param>
    /// <returns>The created movement input.</returns>
    public MovementInput CreateClickToMoveInput(Vector3 targetPosition, float networkTime)
    {
        MovementInput input = MovementInput.CreateClickToMove(
            _nextSequenceNumber++,
            networkTime,
            targetPosition
        );

        AddPendingInput(input);
        return input;
    }

    /// <summary>
    /// Creates a stop input and adds it to the pending queue.
    /// </summary>
    /// <param name="networkTime">The current network-synchronized time.</param>
    /// <returns>The created movement input.</returns>
    public MovementInput CreateStopInput(float networkTime)
    {
        MovementInput input = MovementInput.CreateStop(
            _nextSequenceNumber++,
            networkTime
        );

        AddPendingInput(input);
        return input;
    }

    /// <summary>
    /// Creates a dash input and adds it to the pending queue.
    /// </summary>
    /// <param name="dashData">The dash-specific input data.</param>
    /// <param name="networkTime">The current network-synchronized time.</param>
    /// <returns>The created movement input.</returns>
    public MovementInput CreateDashInput(DashInputData dashData, float networkTime)
    {
        MovementInput input = MovementInput.CreateDash(
            _nextSequenceNumber++,
            networkTime,
            dashData
        );

        AddPendingInput(input);
        return input;
    }

    /// <summary>
    /// Creates a directional movement input and adds it to the pending queue.
    /// </summary>
    /// <param name="direction">The movement direction.</param>
    /// <param name="networkTime">The current network-synchronized time.</param>
    /// <returns>The created movement input.</returns>
    public MovementInput CreateDirectionalInput(Vector3 direction, float networkTime)
    {
        MovementInput input = MovementInput.CreateDirectional(
            _nextSequenceNumber++,
            networkTime,
            direction
        );

        AddPendingInput(input);
        return input;
    }

    /// <summary>
    /// Removes all inputs that have been acknowledged by the server.
    /// </summary>
    /// <param name="lastProcessedSequence">The last sequence number processed by the server.</param>
    public void RemoveAcknowledgedInputs(uint lastProcessedSequence)
    {
        while (_pendingInputs.Count > 0)
        {
            MovementInput oldest = _pendingInputs.Peek();
            if (IsSequenceAcknowledged(oldest.SequenceNumber, lastProcessedSequence))
            {
                _pendingInputs.Dequeue();
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Replays all pending inputs using the provided replayer.
    /// Used after server reconciliation to re-apply unacknowledged inputs.
    /// </summary>
    /// <param name="replayer">The input replayer implementation.</param>
    /// <param name="agentSpeed">The current agent movement speed.</param>
    /// <param name="fixedDeltaTime">The fixed delta time for physics calculations.</param>
    public void ReplayPendingInputs(IInputReplayer replayer, float agentSpeed, float fixedDeltaTime)
    {
        foreach (MovementInput input in _pendingInputs)
        {
            ReplayInput(replayer, input, agentSpeed, fixedDeltaTime);
        }
    }

    /// <summary>
    /// Clears all pending inputs from the queue.
    /// Used when a hard reset or desync is detected.
    /// </summary>
    public void ClearPendingInputs()
    {
        _pendingInputs.Clear();
    }

    /// <summary>
    /// Resets the prediction handler to its initial state.
    /// </summary>
    public void Reset()
    {
        _pendingInputs.Clear();
        _nextSequenceNumber = 0;
    }

    #endregion

    #region Private Methods

    private void AddPendingInput(MovementInput input)
    {
        while (_pendingInputs.Count >= MAX_PENDING_INPUTS)
        {
            _pendingInputs.Dequeue();
        }
        _pendingInputs.Enqueue(input);
    }

    private bool IsSequenceAcknowledged(uint inputSequence, uint lastProcessedSequence)
    {
        return IsSequenceNewer(lastProcessedSequence, inputSequence) ||
               inputSequence == lastProcessedSequence;
    }

    private bool IsSequenceNewer(uint a, uint b)
    {
        return (a > b) && (a - b < uint.MaxValue / 2) ||
               (b > a) && (b - a > uint.MaxValue / 2);
    }

    private void ReplayInput(IInputReplayer replayer, MovementInput input, float agentSpeed, float fixedDeltaTime)
    {
        switch (input.InputType)
        {
            case MovementInputType.ClickToMove:
                replayer.ReplayClickToMove(input.TargetPosition);
                break;

            case MovementInputType.Directional:
                replayer.ReplayDirectional(input.MoveDirection, agentSpeed, fixedDeltaTime);
                break;

            case MovementInputType.Dash:
                replayer.ReplayDash(input.DashData);
                break;

            case MovementInputType.Stop:
                replayer.ReplayStop();
                break;
        }
    }

    #endregion
}
