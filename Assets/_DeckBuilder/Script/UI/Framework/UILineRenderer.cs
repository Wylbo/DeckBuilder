using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a line through a series of points on a Unity UI canvas.
/// Uses a grid-based coordinate system to map points to the rect transform.
/// </summary>
public class UILineRenderer : Graphic
{
    #region Fields

    /// <summary>The grid dimensions used to map point coordinates to rect space.</summary>
    [SerializeField]
    [Tooltip("Number of grid divisions on each axis. Must be greater than zero.")]
    private Vector2 gridSize = new Vector2(1f, 1f);

    /// <summary>The thickness of the rendered line in pixels.</summary>
    [SerializeField]
    [Tooltip("Width of the line in pixels.")]
    private float thickness = 5f;

    /// <summary>When true, points are treated as direct local-space positions without grid scaling.</summary>
    [SerializeField]
    [Tooltip("When enabled, points are used as direct local-space coordinates instead of grid-mapped coordinates.")]
    private bool useDirectCoordinates;

    #endregion

    #region Private Members

    private float _unitWidth;
    private float _unitHeight;

    #endregion

    #region Getters

    /// <summary>
    /// Gets or sets the grid dimensions used to map point coordinates to rect space.
    /// Setting this value marks the mesh as dirty for re-rendering.
    /// </summary>
    public Vector2 GridSize
    {
        get => gridSize;
        set
        {
            gridSize = value;
            SetVerticesDirty();
        }
    }

    /// <summary>
    /// Gets or sets whether points are treated as direct local-space positions.
    /// When true, grid scaling is bypassed and points map directly to local coordinates.
    /// </summary>
    public bool UseDirectCoordinates
    {
        get => useDirectCoordinates;
        set
        {
            useDirectCoordinates = value;
            SetVerticesDirty();
        }
    }

    /// <summary>
    /// Gets or sets the list of points defining the line path in grid coordinates.
    /// </summary>
    [field: SerializeField]
    public List<Vector2> Points { get; set; } = new List<Vector2>();

    #endregion

    #region Unity Message Methods

    /// <summary>
    /// Populates the mesh with vertices and triangles to render the line.
    /// </summary>
    /// <param name="vh">The vertex helper used to build the mesh.</param>
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (Points.Count < 2)
        {
            return;
        }

        if (useDirectCoordinates)
        {
            _unitWidth = 1f;
            _unitHeight = 1f;
        }
        else
        {
            if (gridSize.x == 0f || gridSize.y == 0f)
            {
                return;
            }

            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;
            _unitWidth = width / gridSize.x;
            _unitHeight = height / gridSize.y;
        }

        for (int i = 0; i < Points.Count; i++)
        {
            Vector2 direction = GetPerpendicularDirection(i);
            DrawVerticesForPoint(Points[i], direction, vh);
        }

        for (int i = 0; i < Points.Count - 1; i++)
        {
            int index = i * 2;
            vh.AddTriangle(index, index + 1, index + 3);
            vh.AddTriangle(index + 3, index + 2, index);
        }
    }

    #endregion

    #region Private Methods
    /// <summary>
    /// Computes the perpendicular direction at a given point index.
    /// For endpoints, uses the adjacent segment direction.
    /// For interior points, averages the two adjacent segment directions.
    /// </summary>
    /// <param name="index">The index of the point in the points list.</param>
    /// <returns>A normalized vector perpendicular to the line at the given point.</returns>
    private Vector2 GetPerpendicularDirection(int index)
    {
        Vector2 direction = Vector2.zero;

        if (index > 0)
        {
            direction += (Points[index] - Points[index - 1]).normalized;
        }

        if (index < Points.Count - 1)
        {
            direction += (Points[index + 1] - Points[index]).normalized;
        }

        direction.Normalize();

        return new Vector2(-direction.y, direction.x);
    }

    /// <summary>
    /// Adds two vertices offset perpendicular to the line direction at the given point.
    /// </summary>
    /// <param name="point">The point position in grid coordinates.</param>
    /// <param name="perpendicular">The perpendicular direction for thickness offset.</param>
    /// <param name="vh">The vertex helper to add vertices to.</param>
    private void DrawVerticesForPoint(Vector2 point, Vector2 perpendicular, VertexHelper vh)
    {
        Vector3 center = new Vector3(_unitWidth * point.x, _unitHeight * point.y);
        Vector3 offset = new Vector3(perpendicular.x, perpendicular.y) * (thickness * 0.5f);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = center - offset;
        vh.AddVert(vertex);

        vertex.position = center + offset;
        vh.AddVert(vertex);
    }

    #endregion
}
