using UnityEngine;

/// <summary>
/// Calculates node positions for the map UI layout.
/// Converts grid coordinates (floor, column) to screen positions for horizontal display.
/// </summary>
public class MapLayoutCalculator
{
    #region Private Members

    private readonly float _floorSpacing;
    private readonly float _columnSpacing;
    private readonly float _leftPadding;
    private readonly int _gridWidth;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new layout calculator with the specified spacing parameters.
    /// </summary>
    /// <param name="floorSpacing">Horizontal spacing between floors in pixels.</param>
    /// <param name="columnSpacing">Vertical spacing between columns in pixels.</param>
    /// <param name="leftPadding">Left padding for the first floor.</param>
    /// <param name="gridWidth">The width of the grid (number of columns).</param>
    public MapLayoutCalculator(float floorSpacing, float columnSpacing, float leftPadding, int gridWidth)
    {
        _floorSpacing = floorSpacing;
        _columnSpacing = columnSpacing;
        _leftPadding = leftPadding;
        _gridWidth = gridWidth;
    }

    /// <summary>
    /// Calculates the local position for a node within the content area.
    /// Floors are arranged horizontally (left to right).
    /// Columns are arranged vertically (top to bottom, centered).
    /// </summary>
    /// <param name="floor">The floor index (horizontal position).</param>
    /// <param name="column">The column index (vertical position).</param>
    /// <returns>Local position in the content RectTransform.</returns>
    public Vector2 CalculateNodePosition(int floor, int column)
    {
        float x = _leftPadding + (floor * _floorSpacing);
        float yOffset = (_gridWidth - 1) * _columnSpacing * 0.5f;
        float y = yOffset - (column * _columnSpacing);

        return new Vector2(x, y);
    }

    /// <summary>
    /// Calculates the required content width to fit all floors.
    /// </summary>
    /// <param name="totalFloors">Total number of floors including boss floor.</param>
    /// <returns>The required width in pixels.</returns>
    public float CalculateContentWidth(int totalFloors)
    {
        return (_leftPadding * 2f) + (totalFloors * _floorSpacing);
    }

    /// <summary>
    /// Calculates the required content height to fit all columns.
    /// </summary>
    /// <returns>The required height in pixels.</returns>
    public float CalculateContentHeight()
    {
        return (_gridWidth * _columnSpacing) + (_columnSpacing * 0.5f);
    }

    /// <summary>
    /// Calculates the normalized scroll position for a specific floor.
    /// Used for scrolling to a floor programmatically.
    /// </summary>
    /// <param name="floor">The floor to scroll to.</param>
    /// <param name="totalFloors">Total number of floors.</param>
    /// <param name="viewportWidth">Width of the scroll viewport.</param>
    /// <returns>Normalized scroll position (0-1).</returns>
    public float CalculateScrollPositionForFloor(int floor, int totalFloors, float viewportWidth)
    {
        float contentWidth = CalculateContentWidth(totalFloors);
        float scrollableWidth = contentWidth - viewportWidth;

        if (scrollableWidth <= 0f)
        {
            return 0f;
        }

        float targetX = _leftPadding + (floor * _floorSpacing) - (viewportWidth * 0.5f);
        float normalizedPosition = Mathf.Clamp01(targetX / scrollableWidth);

        return normalizedPosition;
    }

    #endregion
}
