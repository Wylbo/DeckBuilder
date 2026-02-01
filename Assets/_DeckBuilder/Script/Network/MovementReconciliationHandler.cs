using UnityEngine;

/// <summary>
/// Configuration for movement reconciliation behavior.
/// </summary>
public struct ReconciliationConfig
{
    /// <summary>Threshold beyond which position snaps instead of interpolates.</summary>
    public float SnapThreshold;

    /// <summary>Threshold for detecting large position errors.</summary>
    public float LargeErrorThreshold;

    /// <summary>Maximum consecutive large corrections before forcing a hard reset.</summary>
    public int MaxConsecutiveLargeCorrections;

    /// <summary>Minimum error magnitude to trigger any correction.</summary>
    public float MinErrorThreshold;

    /// <summary>
    /// Creates a default reconciliation configuration.
    /// </summary>
    /// <returns>A configuration with default values.</returns>
    public static ReconciliationConfig Default()
    {
        return new ReconciliationConfig
        {
            SnapThreshold = 2f,
            LargeErrorThreshold = 1f,
            MaxConsecutiveLargeCorrections = 5,
            MinErrorThreshold = 0.01f
        };
    }
}

/// <summary>
/// Result of a reconciliation operation indicating what action to take.
/// </summary>
public struct ReconciliationResult
{
    /// <summary>Whether to snap directly to the server position.</summary>
    public bool ShouldSnap;

    /// <summary>Whether to apply smooth correction.</summary>
    public bool ShouldSmooth;

    /// <summary>Whether to replay pending inputs after correction.</summary>
    public bool ShouldReplay;

    /// <summary>Whether to clear all pending inputs due to desync.</summary>
    public bool ShouldClearInputs;

    /// <summary>The corrected position to move to.</summary>
    public Vector3 CorrectedPosition;

    /// <summary>The visual correction offset to apply.</summary>
    public Vector3 CorrectionOffset;

    /// <summary>The position error magnitude.</summary>
    public float ErrorMagnitude;
}

/// <summary>
/// Handles comparison of server state with predicted state and determines correction actions.
/// Manages consecutive large correction tracking for desync detection.
/// </summary>
public class MovementReconciliationHandler
{
    #region Private Members

    private int _consecutiveLargeCorrections;

    #endregion

    #region Getters

    /// <summary>Gets the current count of consecutive large corrections.</summary>
    public int ConsecutiveLargeCorrections => _consecutiveLargeCorrections;

    #endregion

    #region Public Methods

    /// <summary>
    /// Reconciles the predicted position with the authoritative server state.
    /// </summary>
    /// <param name="serverState">The authoritative server state.</param>
    /// <param name="predictedPosition">The client's predicted position.</param>
    /// <param name="previousVisualPosition">The previous visual position for offset calculation.</param>
    /// <param name="config">The reconciliation configuration.</param>
    /// <returns>A result indicating what correction action to take.</returns>
    public ReconciliationResult Reconcile(
        MovementState serverState,
        Vector3 predictedPosition,
        Vector3 previousVisualPosition,
        ReconciliationConfig config)
    {
        Vector3 serverPosition = serverState.Position;
        Vector3 error = CalculatePositionError(serverPosition, predictedPosition);
        float errorMagnitude = error.magnitude;

        ReconciliationResult result = new ReconciliationResult
        {
            CorrectedPosition = serverPosition,
            ErrorMagnitude = errorMagnitude
        };

        if (ShouldSnapToPosition(errorMagnitude, config.SnapThreshold))
        {
            HandleSnapCorrection(ref result);
            return result;
        }

        if (errorMagnitude > config.MinErrorThreshold)
        {
            if (HandleLargeErrorTracking(errorMagnitude, config))
            {
                HandleDesyncReset(ref result);
                return result;
            }

            HandleSmoothCorrection(ref result, previousVisualPosition, serverPosition);
        }

        return result;
    }

    /// <summary>
    /// Calculates the position error between server and predicted positions.
    /// </summary>
    /// <param name="serverPosition">The authoritative server position.</param>
    /// <param name="predictedPosition">The client's predicted position.</param>
    /// <returns>The error vector from predicted to server position.</returns>
    public Vector3 CalculatePositionError(Vector3 serverPosition, Vector3 predictedPosition)
    {
        return serverPosition - predictedPosition;
    }

    /// <summary>
    /// Determines if the error magnitude exceeds the snap threshold.
    /// </summary>
    /// <param name="errorMagnitude">The magnitude of the position error.</param>
    /// <param name="snapThreshold">The threshold for snapping.</param>
    /// <returns>True if should snap; otherwise false.</returns>
    public bool ShouldSnapToPosition(float errorMagnitude, float snapThreshold)
    {
        return errorMagnitude > snapThreshold;
    }

    /// <summary>
    /// Resets the consecutive large correction counter.
    /// </summary>
    public void ResetCorrectionTracking()
    {
        _consecutiveLargeCorrections = 0;
    }

    /// <summary>
    /// Resets the handler to its initial state.
    /// </summary>
    public void Reset()
    {
        _consecutiveLargeCorrections = 0;
    }

    #endregion

    #region Private Methods

    private void HandleSnapCorrection(ref ReconciliationResult result)
    {
        result.ShouldSnap = true;
        result.ShouldSmooth = false;
        result.ShouldReplay = false;
        result.ShouldClearInputs = false;
        result.CorrectionOffset = Vector3.zero;
        _consecutiveLargeCorrections = 0;
    }

    private bool HandleLargeErrorTracking(float errorMagnitude, ReconciliationConfig config)
    {
        if (errorMagnitude > config.LargeErrorThreshold)
        {
            _consecutiveLargeCorrections++;
            return _consecutiveLargeCorrections > config.MaxConsecutiveLargeCorrections;
        }

        _consecutiveLargeCorrections = 0;
        return false;
    }

    private void HandleDesyncReset(ref ReconciliationResult result)
    {
        result.ShouldSnap = true;
        result.ShouldSmooth = false;
        result.ShouldReplay = false;
        result.ShouldClearInputs = true;
        result.CorrectionOffset = Vector3.zero;
        _consecutiveLargeCorrections = 0;
    }

    private void HandleSmoothCorrection(
        ref ReconciliationResult result,
        Vector3 previousVisualPosition,
        Vector3 serverPosition)
    {
        result.ShouldSnap = false;
        result.ShouldSmooth = true;
        result.ShouldReplay = true;
        result.ShouldClearInputs = false;
        result.CorrectionOffset = previousVisualPosition - serverPosition;
    }

    #endregion
}
