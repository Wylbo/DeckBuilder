using UnityEngine;

/// <summary>
/// Manages visual transform smoothing and interpolation for movement.
/// Handles correction offset smoothing for client-side prediction visual feedback.
/// </summary>
public class MovementVisualHandler
{
    #region Private Members

    private Vector3 _correctionOffset = Vector3.zero;

    #endregion

    #region Getters

    /// <summary>Gets the current correction offset between visual and simulation positions.</summary>
    public Vector3 CorrectionOffset => _correctionOffset;

    #endregion

    #region Public Methods

    /// <summary>
    /// Updates the owner's visual position with smooth correction offset interpolation.
    /// </summary>
    /// <param name="visualTransform">The visual transform to update, or null to skip.</param>
    /// <param name="simulationPosition">The current simulation position.</param>
    /// <param name="simulationRotation">The current simulation rotation.</param>
    /// <param name="smoothingRate">The rate at which to smooth the correction offset.</param>
    /// <param name="deltaTime">The time delta for interpolation.</param>
    public void UpdateOwnerVisual(
        Transform visualTransform,
        Vector3 simulationPosition,
        Quaternion simulationRotation,
        float smoothingRate,
        float deltaTime)
    {
        SmoothCorrectionOffset(smoothingRate, deltaTime);
        ApplyVisualPosition(visualTransform, simulationPosition, simulationRotation);
    }

    /// <summary>
    /// Applies the correction offset from a reconciliation result.
    /// Called after server reconciliation determines a smooth correction is needed.
    /// </summary>
    /// <param name="previousVisualPosition">The visual position before reconciliation.</param>
    /// <param name="newSimulationPosition">The new simulation position after reconciliation.</param>
    public void ApplyCorrectionOffset(Vector3 previousVisualPosition, Vector3 newSimulationPosition)
    {
        _correctionOffset = previousVisualPosition - newSimulationPosition;
    }

    /// <summary>
    /// Sets the correction offset directly.
    /// </summary>
    /// <param name="offset">The offset to set.</param>
    public void SetCorrectionOffset(Vector3 offset)
    {
        _correctionOffset = offset;
    }

    /// <summary>
    /// Resets the correction offset to zero.
    /// Used after snap corrections or during dashing.
    /// </summary>
    public void ResetCorrectionOffset()
    {
        _correctionOffset = Vector3.zero;
    }

    /// <summary>
    /// Synchronizes the visual transform directly to the simulation position without offset.
    /// Used during dashing or after snap corrections.
    /// </summary>
    /// <param name="visualTransform">The visual transform to sync.</param>
    /// <param name="position">The position to sync to.</param>
    /// <param name="rotation">The rotation to sync to.</param>
    public void SyncVisualToSimulation(Transform visualTransform, Vector3 position, Quaternion rotation)
    {
        if (visualTransform == null)
        {
            return;
        }

        visualTransform.position = position;
        visualTransform.rotation = rotation;
    }

    /// <summary>
    /// Immediately applies the current visual position with correction offset.
    /// </summary>
    /// <param name="visualTransform">The visual transform to update.</param>
    /// <param name="simulationPosition">The simulation position.</param>
    /// <param name="simulationRotation">The simulation rotation.</param>
    public void UpdateVisualPositionImmediate(
        Transform visualTransform,
        Vector3 simulationPosition,
        Quaternion simulationRotation)
    {
        ApplyVisualPosition(visualTransform, simulationPosition, simulationRotation);
    }

    /// <summary>
    /// Calculates the current visual position including correction offset.
    /// </summary>
    /// <param name="simulationPosition">The simulation position.</param>
    /// <returns>The visual position with offset applied.</returns>
    public Vector3 GetVisualPosition(Vector3 simulationPosition)
    {
        return simulationPosition + _correctionOffset;
    }

    /// <summary>
    /// Resets the handler to its initial state.
    /// </summary>
    public void Reset()
    {
        _correctionOffset = Vector3.zero;
    }

    #endregion

    #region Private Methods

    private void SmoothCorrectionOffset(float smoothingRate, float deltaTime)
    {
        _correctionOffset = Vector3.Lerp(
            _correctionOffset,
            Vector3.zero,
            smoothingRate * deltaTime
        );
    }

    private void ApplyVisualPosition(
        Transform visualTransform,
        Vector3 simulationPosition,
        Quaternion simulationRotation)
    {
        if (visualTransform == null)
        {
            return;
        }

        visualTransform.position = simulationPosition + _correctionOffset;
        visualTransform.rotation = simulationRotation;
    }

    #endregion
}
