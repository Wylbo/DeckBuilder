using UnityEngine;

/// <summary>
/// Renders a curved connection between two map nodes using LineRenderer.
/// Uses quadratic Bezier curves for smooth, natural-looking paths.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MapConnectionRenderer : MonoBehaviour
{
    #region Fields

    [SerializeField]
    [Tooltip("Number of segments for the curve (higher = smoother)")]
    private int curveResolution = 20;

    [SerializeField]
    [Tooltip("Width of the connection line at the start")]
    private float startWidth = 4f;

    [SerializeField]
    [Tooltip("Width of the connection line at the end")]
    private float endWidth = 4f;

    [SerializeField]
    [Tooltip("Color for accessible connections")]
    private Color accessibleColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    [SerializeField]
    [Tooltip("Color for inaccessible connections")]
    private Color inaccessibleColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);

    [SerializeField]
    [Tooltip("Color for visited connections")]
    private Color visitedColor = new Color(0.7f, 0.9f, 0.7f, 1f);

    #endregion

    #region Private Members

    private LineRenderer _lineRenderer;
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private bool _isAccessible;
    private bool _isVisited;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the start position of this connection.
    /// </summary>
    public Vector3 StartPosition => _startPosition;

    /// <summary>
    /// Gets the end position of this connection.
    /// </summary>
    public Vector3 EndPosition => _endPosition;

    #endregion

    #region Unity Message Methods

    private void Awake()
    {
        InitializeLineRenderer();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets up the connection between two positions and renders the curve.
    /// </summary>
    /// <param name="startPosition">World position of the starting node.</param>
    /// <param name="endPosition">World position of the ending node.</param>
    /// <param name="isAccessible">Whether this connection leads to an accessible node.</param>
    /// <param name="isVisited">Whether this connection has been traversed.</param>
    public void SetConnection(Vector3 startPosition, Vector3 endPosition, bool isAccessible, bool isVisited)
    {
        _startPosition = startPosition;
        _endPosition = endPosition;
        _isAccessible = isAccessible;
        _isVisited = isVisited;

        RenderCurve();
        UpdateColor();
    }

    /// <summary>
    /// Updates the accessibility state of this connection.
    /// </summary>
    /// <param name="isAccessible">Whether this connection leads to an accessible node.</param>
    public void UpdateAccessibility(bool isAccessible)
    {
        _isAccessible = isAccessible;
        UpdateColor();
    }

    /// <summary>
    /// Updates the visited state of this connection.
    /// </summary>
    /// <param name="isVisited">Whether this connection has been traversed.</param>
    public void UpdateVisited(bool isVisited)
    {
        _isVisited = isVisited;
        UpdateColor();
    }

    /// <summary>
    /// Resets the connection for reuse from the pool.
    /// </summary>
    public void Reset()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.positionCount = 0;
        }

        _isAccessible = false;
        _isVisited = false;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the LineRenderer component with default settings.
    /// </summary>
    private void InitializeLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer == null)
        {
            Debug.LogError("MapConnectionRenderer: LineRenderer component not found.", this);
            return;
        }

        _lineRenderer.useWorldSpace = false;
        _lineRenderer.startWidth = startWidth;
        _lineRenderer.endWidth = endWidth;
        _lineRenderer.numCapVertices = 4;
        _lineRenderer.numCornerVertices = 4;
    }

    /// <summary>
    /// Renders the Bezier curve between start and end positions.
    /// Converts world positions to local space for scroll view compatibility.
    /// </summary>
    private void RenderCurve()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        Vector3 localStart = transform.InverseTransformPoint(_startPosition);
        Vector3 localEnd = transform.InverseTransformPoint(_endPosition);

        Vector3[] curvePoints = BezierCurveCalculator.CalculateQuadraticBezier(
            localStart,
            localEnd,
            curveResolution);

        _lineRenderer.positionCount = curvePoints.Length;
        _lineRenderer.SetPositions(curvePoints);
    }

    /// <summary>
    /// Updates the line color based on current state.
    /// </summary>
    private void UpdateColor()
    {
        if (_lineRenderer == null)
        {
            return;
        }

        Color lineColor;

        if (_isVisited)
        {
            lineColor = visitedColor;
        }
        else if (_isAccessible)
        {
            lineColor = accessibleColor;
        }
        else
        {
            lineColor = inaccessibleColor;
        }

        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
    }

    #endregion
}
