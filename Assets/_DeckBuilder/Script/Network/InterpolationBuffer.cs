using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single position snapshot for interpolation.
/// </summary>
public struct PositionSnapshot
{
    #region Fields

    /// <summary>Network timestamp when this snapshot was recorded.</summary>
    public float Timestamp;

    /// <summary>World position at this snapshot.</summary>
    public Vector3 Position;

    /// <summary>Rotation at this snapshot.</summary>
    public Quaternion Rotation;

    /// <summary>Velocity at this snapshot.</summary>
    public Vector3 Velocity;

    /// <summary>Whether the entity was moving at this snapshot.</summary>
    public bool IsMoving;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a snapshot from a movement state.
    /// </summary>
    /// <param name="state">The movement state to create a snapshot from.</param>
    /// <returns>A configured PositionSnapshot.</returns>
    public static PositionSnapshot FromMovementState(MovementState state)
    {
        return new PositionSnapshot
        {
            Timestamp = state.ServerTimestamp,
            Position = state.Position,
            Rotation = state.Rotation,
            Velocity = state.Velocity,
            IsMoving = state.IsMoving
        };
    }

    /// <summary>
    /// Interpolates between this snapshot and another.
    /// </summary>
    /// <param name="other">The target snapshot.</param>
    /// <param name="t">Interpolation factor (0-1).</param>
    /// <returns>An interpolated snapshot.</returns>
    public PositionSnapshot Interpolate(PositionSnapshot other, float t)
    {
        return new PositionSnapshot
        {
            Timestamp = Mathf.Lerp(Timestamp, other.Timestamp, t),
            Position = Vector3.Lerp(Position, other.Position, t),
            Rotation = Quaternion.Slerp(Rotation, other.Rotation, t),
            Velocity = Vector3.Lerp(Velocity, other.Velocity, t),
            IsMoving = t < 0.5f ? IsMoving : other.IsMoving
        };
    }

    #endregion
}

/// <summary>
/// Buffers position snapshots from the server and provides smooth interpolation
/// for non-owner clients. Renders entities slightly in the past to ensure smooth visuals.
/// </summary>
public class InterpolationBuffer
{
    #region Fields

    private const float DEFAULT_INTERPOLATION_DELAY = 0.1f;
    private const float MIN_INTERPOLATION_DELAY = 0.05f;
    private const float MAX_INTERPOLATION_DELAY = 0.3f;
    private const float EXTRAPOLATION_LIMIT = 0.25f;
    private const float TELEPORT_THRESHOLD = 5f;
    private const int MAX_BUFFER_SIZE = 32;
    private const float DELAY_ADAPTATION_RATE = 0.1f;

    #endregion

    #region Private Members

    private readonly List<PositionSnapshot> _snapshots = new List<PositionSnapshot>();
    private float _interpolationDelay = DEFAULT_INTERPOLATION_DELAY;
    private float _lastPacketInterval = 0f;
    private float _lastPacketTime = 0f;
    private float _jitterEstimate = 0f;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the current interpolation delay in seconds.
    /// </summary>
    public float InterpolationDelay => _interpolationDelay;

    /// <summary>
    /// Gets the number of snapshots in the buffer.
    /// </summary>
    public int SnapshotCount => _snapshots.Count;

    /// <summary>
    /// Gets a value indicating whether the buffer has enough data for interpolation.
    /// </summary>
    public bool HasSufficientData => _snapshots.Count >= 2;

    /// <summary>
    /// Gets the estimated network jitter.
    /// </summary>
    public float JitterEstimate => _jitterEstimate;

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new snapshot to the buffer.
    /// </summary>
    /// <param name="snapshot">The snapshot to add.</param>
    public void AddSnapshot(PositionSnapshot snapshot)
    {
        UpdateJitterEstimate(snapshot.Timestamp);

        if (_snapshots.Count > 0)
        {
            PositionSnapshot lastSnapshot = _snapshots[_snapshots.Count - 1];

            if (snapshot.Timestamp <= lastSnapshot.Timestamp)
            {
                return;
            }

            Vector3 positionDelta = snapshot.Position - lastSnapshot.Position;
            if (positionDelta.sqrMagnitude > TELEPORT_THRESHOLD * TELEPORT_THRESHOLD)
            {
                _snapshots.Clear();
            }
        }

        _snapshots.Add(snapshot);

        while (_snapshots.Count > MAX_BUFFER_SIZE)
        {
            _snapshots.RemoveAt(0);
        }

        AdaptInterpolationDelay();
    }

    /// <summary>
    /// Adds a snapshot from a movement state.
    /// </summary>
    /// <param name="state">The movement state to add.</param>
    public void AddSnapshot(MovementState state)
    {
        AddSnapshot(PositionSnapshot.FromMovementState(state));
    }

    /// <summary>
    /// Gets the interpolated position for the current render time.
    /// </summary>
    /// <param name="currentNetworkTime">The current network time.</param>
    /// <returns>The interpolated snapshot, or the last known snapshot if interpolation is not possible.</returns>
    public PositionSnapshot GetInterpolatedSnapshot(float currentNetworkTime)
    {
        if (_snapshots.Count == 0)
        {
            return default;
        }

        float renderTime = currentNetworkTime - _interpolationDelay;

        if (_snapshots.Count == 1)
        {
            return ExtrapolateFromSingle(_snapshots[0], renderTime);
        }

        int beforeIndex = -1;
        int afterIndex = -1;

        for (int i = 0; i < _snapshots.Count - 1; i++)
        {
            if (_snapshots[i].Timestamp <= renderTime && _snapshots[i + 1].Timestamp >= renderTime)
            {
                beforeIndex = i;
                afterIndex = i + 1;
                break;
            }
        }

        if (beforeIndex >= 0 && afterIndex >= 0)
        {
            return InterpolateBetweenSnapshots(beforeIndex, afterIndex, renderTime);
        }

        if (renderTime < _snapshots[0].Timestamp)
        {
            return _snapshots[0];
        }

        return ExtrapolateFromLast(renderTime);
    }

    /// <summary>
    /// Gets the interpolated position for the current render time.
    /// </summary>
    /// <param name="currentNetworkTime">The current network time.</param>
    /// <returns>The interpolated world position.</returns>
    public Vector3 GetInterpolatedPosition(float currentNetworkTime)
    {
        return GetInterpolatedSnapshot(currentNetworkTime).Position;
    }

    /// <summary>
    /// Gets the interpolated rotation for the current render time.
    /// </summary>
    /// <param name="currentNetworkTime">The current network time.</param>
    /// <returns>The interpolated rotation.</returns>
    public Quaternion GetInterpolatedRotation(float currentNetworkTime)
    {
        return GetInterpolatedSnapshot(currentNetworkTime).Rotation;
    }

    /// <summary>
    /// Clears all snapshots from the buffer.
    /// </summary>
    public void Clear()
    {
        _snapshots.Clear();
        _interpolationDelay = DEFAULT_INTERPOLATION_DELAY;
        _jitterEstimate = 0f;
        _lastPacketTime = 0f;
    }

    /// <summary>
    /// Forces a specific interpolation delay.
    /// </summary>
    /// <param name="delay">The delay to set.</param>
    public void SetInterpolationDelay(float delay)
    {
        _interpolationDelay = Mathf.Clamp(delay, MIN_INTERPOLATION_DELAY, MAX_INTERPOLATION_DELAY);
    }

    /// <summary>
    /// Removes old snapshots that are no longer needed for interpolation.
    /// </summary>
    /// <param name="currentNetworkTime">The current network time.</param>
    public void PruneOldSnapshots(float currentNetworkTime)
    {
        float cutoffTime = currentNetworkTime - _interpolationDelay - 0.5f;

        while (_snapshots.Count > 2 && _snapshots[0].Timestamp < cutoffTime)
        {
            _snapshots.RemoveAt(0);
        }
    }

    #endregion

    #region Private Methods

    private PositionSnapshot InterpolateBetweenSnapshots(int beforeIndex, int afterIndex, float renderTime)
    {
        PositionSnapshot before = _snapshots[beforeIndex];
        PositionSnapshot after = _snapshots[afterIndex];

        float duration = after.Timestamp - before.Timestamp;
        if (duration <= 0f)
        {
            return before;
        }

        float t = (renderTime - before.Timestamp) / duration;
        t = Mathf.Clamp01(t);

        return before.Interpolate(after, t);
    }

    private PositionSnapshot ExtrapolateFromSingle(PositionSnapshot snapshot, float renderTime)
    {
        float timeDelta = renderTime - snapshot.Timestamp;

        if (timeDelta <= 0f || !snapshot.IsMoving)
        {
            return snapshot;
        }

        timeDelta = Mathf.Min(timeDelta, EXTRAPOLATION_LIMIT);

        return new PositionSnapshot
        {
            Timestamp = renderTime,
            Position = snapshot.Position + snapshot.Velocity * timeDelta,
            Rotation = snapshot.Rotation,
            Velocity = snapshot.Velocity,
            IsMoving = snapshot.IsMoving
        };
    }

    private PositionSnapshot ExtrapolateFromLast(float renderTime)
    {
        if (_snapshots.Count < 2)
        {
            return _snapshots[_snapshots.Count - 1];
        }

        PositionSnapshot last = _snapshots[_snapshots.Count - 1];
        PositionSnapshot secondLast = _snapshots[_snapshots.Count - 2];

        float timeDelta = renderTime - last.Timestamp;

        if (timeDelta <= 0f)
        {
            return last;
        }

        timeDelta = Mathf.Min(timeDelta, EXTRAPOLATION_LIMIT);

        Vector3 velocity = last.Velocity;
        if (velocity.sqrMagnitude < 0.01f && last.IsMoving)
        {
            float snapshotDuration = last.Timestamp - secondLast.Timestamp;
            if (snapshotDuration > 0f)
            {
                velocity = (last.Position - secondLast.Position) / snapshotDuration;
            }
        }

        return new PositionSnapshot
        {
            Timestamp = renderTime,
            Position = last.Position + velocity * timeDelta,
            Rotation = last.Rotation,
            Velocity = velocity,
            IsMoving = last.IsMoving
        };
    }

    private void UpdateJitterEstimate(float packetTimestamp)
    {
        if (_lastPacketTime > 0f)
        {
            float interval = packetTimestamp - _lastPacketTime;
            float expectedInterval = _lastPacketInterval > 0f ? _lastPacketInterval : interval;
            float deviation = Mathf.Abs(interval - expectedInterval);

            _jitterEstimate = Mathf.Lerp(_jitterEstimate, deviation, 0.1f);
            _lastPacketInterval = Mathf.Lerp(_lastPacketInterval, interval, 0.2f);
        }

        _lastPacketTime = packetTimestamp;
    }

    private void AdaptInterpolationDelay()
    {
        float targetDelay = DEFAULT_INTERPOLATION_DELAY + _jitterEstimate * 2f;
        targetDelay = Mathf.Clamp(targetDelay, MIN_INTERPOLATION_DELAY, MAX_INTERPOLATION_DELAY);

        _interpolationDelay = Mathf.Lerp(_interpolationDelay, targetDelay, DELAY_ADAPTATION_RATE);
    }

    #endregion
}
