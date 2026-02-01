using UnityEngine;

/// <summary>
/// Tracks round-trip time (RTT) for network latency estimation.
/// Uses exponential moving average for smooth RTT values.
/// </summary>
public class RttTracker
{
    #region Fields

    private const float DEFAULT_RTT = 0.1f;
    private const float RTT_SMOOTHING = 0.1f;
    private const float MIN_RTT = 0.001f;
    private const float MAX_RTT = 2f;

    #endregion

    #region Private Members

    private float _smoothedRtt;
    private float _rttVariance;
    private float _lastMeasurementTime;
    private int _sampleCount;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the smoothed RTT value in seconds.
    /// </summary>
    public float SmoothedRtt => _smoothedRtt;

    /// <summary>
    /// Gets the RTT variance for jitter estimation.
    /// </summary>
    public float RttVariance => _rttVariance;

    /// <summary>
    /// Gets the estimated jitter based on RTT variance.
    /// </summary>
    public float EstimatedJitter => Mathf.Sqrt(_rttVariance);

    /// <summary>
    /// Gets the number of RTT samples collected.
    /// </summary>
    public int SampleCount => _sampleCount;

    /// <summary>
    /// Gets half the RTT, representing one-way latency estimate.
    /// </summary>
    public float OneWayLatency => _smoothedRtt / 2f;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new RTT tracker with default initial RTT.
    /// </summary>
    public RttTracker() : this(DEFAULT_RTT)
    {
    }

    /// <summary>
    /// Creates a new RTT tracker with specified initial RTT.
    /// </summary>
    /// <param name="initialRtt">Initial RTT estimate in seconds.</param>
    public RttTracker(float initialRtt)
    {
        _smoothedRtt = Mathf.Clamp(initialRtt, MIN_RTT, MAX_RTT);
        _rttVariance = 0f;
        _sampleCount = 0;
    }

    /// <summary>
    /// Updates the RTT estimate with a new measurement.
    /// </summary>
    /// <param name="measuredRtt">The measured RTT in seconds.</param>
    public void UpdateRtt(float measuredRtt)
    {
        measuredRtt = Mathf.Clamp(measuredRtt, MIN_RTT, MAX_RTT);

        if (_sampleCount == 0)
        {
            _smoothedRtt = measuredRtt;
            _rttVariance = 0f;
        }
        else
        {
            float diff = measuredRtt - _smoothedRtt;
            _smoothedRtt = Mathf.Lerp(_smoothedRtt, measuredRtt, RTT_SMOOTHING);
            _rttVariance = Mathf.Lerp(_rttVariance, diff * diff, RTT_SMOOTHING);
        }

        _sampleCount++;
        _lastMeasurementTime = Time.time;
    }

    /// <summary>
    /// Updates RTT based on a server state timestamp.
    /// </summary>
    /// <param name="serverTimestamp">The timestamp from the server state.</param>
    /// <param name="currentNetworkTime">The current network time.</param>
    public void OnStateReceived(float serverTimestamp, float currentNetworkTime)
    {
        float rtt = currentNetworkTime - serverTimestamp;
        if (rtt > 0f)
        {
            UpdateRtt(rtt);
        }
    }

    /// <summary>
    /// Resets the RTT tracker to initial state.
    /// </summary>
    public void Reset()
    {
        _smoothedRtt = DEFAULT_RTT;
        _rttVariance = 0f;
        _sampleCount = 0;
    }

    /// <summary>
    /// Calculates the recommended interpolation delay based on RTT and jitter.
    /// </summary>
    /// <param name="baseDelay">Base interpolation delay in seconds.</param>
    /// <returns>Recommended interpolation delay accounting for latency and jitter.</returns>
    public float CalculateRecommendedInterpolationDelay(float baseDelay)
    {
        float jitterBuffer = EstimatedJitter * 2f;
        return Mathf.Max(baseDelay, OneWayLatency + jitterBuffer);
    }

    /// <summary>
    /// Calculates the client time that corresponds to a server timestamp,
    /// accounting for one-way latency.
    /// </summary>
    /// <param name="serverTimestamp">The server timestamp.</param>
    /// <returns>The estimated local time when the event occurred.</returns>
    public float EstimateLocalTimeFromServerTime(float serverTimestamp)
    {
        return serverTimestamp + OneWayLatency;
    }

    /// <summary>
    /// Calculates the server time that corresponds to a local timestamp,
    /// accounting for one-way latency.
    /// </summary>
    /// <param name="localTimestamp">The local timestamp.</param>
    /// <returns>The estimated server time when the event will be received.</returns>
    public float EstimateServerTimeFromLocalTime(float localTimestamp)
    {
        return localTimestamp + OneWayLatency;
    }

    #endregion
}
