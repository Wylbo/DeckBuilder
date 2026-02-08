using UnityEngine;

/// <summary>
/// Static utility class for calculating Bezier curve points.
/// Used for generating smooth curved paths between map nodes.
/// </summary>
public static class BezierCurveCalculator
{
    #region Public Methods

    /// <summary>
    /// Calculates points along a quadratic Bezier curve between two positions.
    /// </summary>
    /// <param name="start">Starting point of the curve.</param>
    /// <param name="end">Ending point of the curve.</param>
    /// <param name="resolution">Number of segments to generate (higher = smoother).</param>
    /// <returns>Array of positions along the curve.</returns>
    public static Vector3[] CalculateQuadraticBezier(Vector3 start, Vector3 end, int resolution)
    {
        Vector3 controlPoint = CalculateControlPoint(start, end);
        Vector3[] points = new Vector3[resolution + 1];

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            points[i] = EvaluateQuadraticPoint(start, controlPoint, end, t);
        }

        return points;
    }

    /// <summary>
    /// Calculates points along a quadratic Bezier curve with a custom control point.
    /// </summary>
    /// <param name="start">Starting point of the curve.</param>
    /// <param name="controlPoint">Control point that influences the curve shape.</param>
    /// <param name="end">Ending point of the curve.</param>
    /// <param name="resolution">Number of segments to generate.</param>
    /// <returns>Array of positions along the curve.</returns>
    public static Vector3[] CalculateQuadraticBezierWithControl(
        Vector3 start,
        Vector3 controlPoint,
        Vector3 end,
        int resolution)
    {
        Vector3[] points = new Vector3[resolution + 1];

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            points[i] = EvaluateQuadraticPoint(start, controlPoint, end, t);
        }

        return points;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Calculates an appropriate control point for a connection between two nodes.
    /// Creates a natural curved path, with more curvature for diagonal connections.
    /// </summary>
    /// <param name="start">Starting point.</param>
    /// <param name="end">Ending point.</param>
    /// <returns>The calculated control point.</returns>
    private static Vector3 CalculateControlPoint(Vector3 start, Vector3 end)
    {
        Vector3 midPoint = (start + end) * 0.5f;
        float horizontalDistance = end.x - start.x;
        float verticalDifference = Mathf.Abs(end.y - start.y);

        float horizontalOffset = horizontalDistance * 0.3f;
        float verticalOffset = verticalDifference * 0.2f;

        return midPoint + new Vector3(horizontalOffset, verticalOffset, 0f);
    }

    /// <summary>
    /// Evaluates a point on a quadratic Bezier curve at parameter t.
    /// Uses the formula: B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
    /// </summary>
    /// <param name="p0">Start point.</param>
    /// <param name="p1">Control point.</param>
    /// <param name="p2">End point.</param>
    /// <param name="t">Parameter value between 0 and 1.</param>
    /// <returns>The point on the curve at parameter t.</returns>
    private static Vector3 EvaluateQuadraticPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float oneMinusT = 1f - t;
        float oneMinusTSquared = oneMinusT * oneMinusT;
        float tSquared = t * t;

        return (oneMinusTSquared * p0) +
               (2f * oneMinusT * t * p1) +
               (tSquared * p2);
    }

    #endregion
}
