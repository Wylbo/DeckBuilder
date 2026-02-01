using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single historical position entry for lag compensation.
/// </summary>
public struct PositionHistoryEntry
{
    #region Fields

    /// <summary>Network timestamp when this entry was recorded.</summary>
    public float Timestamp;

    /// <summary>World position at this entry.</summary>
    public Vector3 Position;

    /// <summary>Rotation at this entry.</summary>
    public Quaternion Rotation;

    /// <summary>Bounds of hitboxes at this entry for hit detection.</summary>
    public Bounds HitboxBounds;

    #endregion

    #region Public Methods

    /// <summary>
    /// Interpolates between this entry and another.
    /// </summary>
    /// <param name="other">The target entry.</param>
    /// <param name="t">Interpolation factor (0-1).</param>
    /// <returns>An interpolated entry.</returns>
    public PositionHistoryEntry Interpolate(PositionHistoryEntry other, float t)
    {
        return new PositionHistoryEntry
        {
            Timestamp = Mathf.Lerp(Timestamp, other.Timestamp, t),
            Position = Vector3.Lerp(Position, other.Position, t),
            Rotation = Quaternion.Slerp(Rotation, other.Rotation, t),
            HitboxBounds = InterpolateBounds(HitboxBounds, other.HitboxBounds, t)
        };
    }

    #endregion

    #region Private Methods

    private static Bounds InterpolateBounds(Bounds a, Bounds b, float t)
    {
        return new Bounds(
            Vector3.Lerp(a.center, b.center, t),
            Vector3.Lerp(a.size, b.size, t)
        );
    }

    #endregion
}

/// <summary>
/// Server-side component that records position history for lag compensation.
/// Allows the server to rewind entity positions to perform hit detection
/// from the perspective of a lagged client.
/// </summary>
public class PositionHistory
{
    #region Fields

    private const float DEFAULT_HISTORY_DURATION = 1f;
    private const int MAX_HISTORY_SIZE = 128;
    private const float MIN_RECORD_INTERVAL = 0.01f;

    #endregion

    #region Private Members

    private readonly List<PositionHistoryEntry> _history = new List<PositionHistoryEntry>();
    private readonly float _historyDuration;
    private float _lastRecordTime = -1f;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the number of entries in the history buffer.
    /// </summary>
    public int EntryCount => _history.Count;

    /// <summary>
    /// Gets the oldest timestamp in the history.
    /// </summary>
    public float OldestTimestamp => _history.Count > 0 ? _history[0].Timestamp : 0f;

    /// <summary>
    /// Gets the newest timestamp in the history.
    /// </summary>
    public float NewestTimestamp => _history.Count > 0 ? _history[_history.Count - 1].Timestamp : 0f;

    /// <summary>
    /// Gets the configured history duration in seconds.
    /// </summary>
    public float HistoryDuration => _historyDuration;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new position history with the default duration.
    /// </summary>
    public PositionHistory() : this(DEFAULT_HISTORY_DURATION)
    {
    }

    /// <summary>
    /// Creates a new position history with the specified duration.
    /// </summary>
    /// <param name="historyDuration">How long to keep history in seconds.</param>
    public PositionHistory(float historyDuration)
    {
        _historyDuration = Mathf.Max(0.1f, historyDuration);
    }

    /// <summary>
    /// Records a new position entry in the history.
    /// </summary>
    /// <param name="timestamp">Network timestamp for this entry.</param>
    /// <param name="position">World position.</param>
    /// <param name="rotation">Rotation.</param>
    /// <param name="hitboxBounds">Bounds of hitboxes for hit detection.</param>
    public void RecordPosition(float timestamp, Vector3 position, Quaternion rotation, Bounds hitboxBounds)
    {
        if (timestamp - _lastRecordTime < MIN_RECORD_INTERVAL)
        {
            return;
        }

        PositionHistoryEntry entry = new PositionHistoryEntry
        {
            Timestamp = timestamp,
            Position = position,
            Rotation = rotation,
            HitboxBounds = hitboxBounds
        };

        _history.Add(entry);
        _lastRecordTime = timestamp;

        PruneOldEntries(timestamp);
    }

    /// <summary>
    /// Records a new position entry without hitbox bounds.
    /// </summary>
    /// <param name="timestamp">Network timestamp for this entry.</param>
    /// <param name="position">World position.</param>
    /// <param name="rotation">Rotation.</param>
    public void RecordPosition(float timestamp, Vector3 position, Quaternion rotation)
    {
        RecordPosition(timestamp, position, rotation, new Bounds(position, Vector3.one));
    }

    /// <summary>
    /// Gets the interpolated position at a specific timestamp in the past.
    /// Used for lag compensation during hit detection.
    /// </summary>
    /// <param name="timestamp">The timestamp to query.</param>
    /// <param name="entry">The interpolated history entry.</param>
    /// <returns>True if a valid entry was found; otherwise false.</returns>
    public bool TryGetStateAtTime(float timestamp, out PositionHistoryEntry entry)
    {
        entry = default;

        if (_history.Count == 0)
        {
            return false;
        }

        if (timestamp <= _history[0].Timestamp)
        {
            entry = _history[0];
            return true;
        }

        if (timestamp >= _history[_history.Count - 1].Timestamp)
        {
            entry = _history[_history.Count - 1];
            return true;
        }

        for (int i = 0; i < _history.Count - 1; i++)
        {
            if (_history[i].Timestamp <= timestamp && _history[i + 1].Timestamp >= timestamp)
            {
                float duration = _history[i + 1].Timestamp - _history[i].Timestamp;
                if (duration <= 0f)
                {
                    entry = _history[i];
                    return true;
                }

                float t = (timestamp - _history[i].Timestamp) / duration;
                entry = _history[i].Interpolate(_history[i + 1], t);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the position at a specific timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to query.</param>
    /// <returns>The interpolated position, or Vector3.zero if not found.</returns>
    public Vector3 GetPositionAtTime(float timestamp)
    {
        if (TryGetStateAtTime(timestamp, out PositionHistoryEntry entry))
        {
            return entry.Position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Gets the hitbox bounds at a specific timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to query.</param>
    /// <returns>The interpolated bounds, or default if not found.</returns>
    public Bounds GetHitboxBoundsAtTime(float timestamp)
    {
        if (TryGetStateAtTime(timestamp, out PositionHistoryEntry entry))
        {
            return entry.HitboxBounds;
        }
        return default;
    }

    /// <summary>
    /// Clears all history entries.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _lastRecordTime = -1f;
    }

    /// <summary>
    /// Checks if a timestamp is within the recorded history range.
    /// </summary>
    /// <param name="timestamp">The timestamp to check.</param>
    /// <returns>True if the timestamp is within range; otherwise false.</returns>
    public bool IsTimestampInRange(float timestamp)
    {
        if (_history.Count == 0)
        {
            return false;
        }

        return timestamp >= _history[0].Timestamp && timestamp <= _history[_history.Count - 1].Timestamp;
    }

    /// <summary>
    /// Gets all entries within a time range for batch processing.
    /// </summary>
    /// <param name="startTime">Start of the time range.</param>
    /// <param name="endTime">End of the time range.</param>
    /// <param name="results">List to populate with matching entries.</param>
    public void GetEntriesInRange(float startTime, float endTime, List<PositionHistoryEntry> results)
    {
        results.Clear();

        for (int i = 0; i < _history.Count; i++)
        {
            if (_history[i].Timestamp >= startTime && _history[i].Timestamp <= endTime)
            {
                results.Add(_history[i]);
            }
        }
    }

    #endregion

    #region Private Methods

    private void PruneOldEntries(float currentTimestamp)
    {
        float cutoffTime = currentTimestamp - _historyDuration;

        while (_history.Count > 0 && _history[0].Timestamp < cutoffTime)
        {
            _history.RemoveAt(0);
        }

        while (_history.Count > MAX_HISTORY_SIZE)
        {
            _history.RemoveAt(0);
        }
    }

    #endregion
}
