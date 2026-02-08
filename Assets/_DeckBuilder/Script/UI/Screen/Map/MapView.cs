using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main UI view for displaying the roguelike map.
/// Manages horizontal scrolling, node instantiation, and connection rendering.
/// </summary>
public class MapView : UIView
{
    #region Fields

    [Header("Configuration")]
    [SerializeField]
    [Tooltip("Configuration for node type sprites")]
    private MapNodeSpriteConfig spriteConfig;

    [Header("UI References")]
    [SerializeField]
    [Tooltip("ScrollRect for horizontal map scrolling")]
    private ScrollRect scrollRect;

    [SerializeField]
    [Tooltip("Content RectTransform where nodes are spawned")]
    private RectTransform contentRoot;

    [SerializeField]
    [Tooltip("Transform for connection LineRenderers")]
    private Transform connectionRoot;

    [Header("Prefabs")]
    [SerializeField]
    [Tooltip("Prefab for individual map nodes")]
    private MapNodeView nodeViewPrefab;

    [SerializeField]
    [Tooltip("Prefab for connection LineRenderers")]
    private MapConnectionRenderer connectionPrefab;

    [Header("Layout Settings")]
    [SerializeField]
    [Tooltip("Horizontal spacing between floors in pixels")]
    private float floorSpacing = 200f;

    [SerializeField]
    [Tooltip("Vertical spacing between columns in pixels")]
    private float columnSpacing = 120f;

    [SerializeField]
    [Tooltip("Left padding for the first floor")]
    private float leftPadding = 150f;

    [SerializeField]
    [Tooltip("Duration of scroll animation in seconds")]
    private float scrollAnimationDuration = 0.5f;

    #endregion

    #region Private Members

    private readonly Dictionary<(int floor, int column), MapNodeView> _spawnedNodes =
        new Dictionary<(int floor, int column), MapNodeView>();

    private readonly List<MapConnectionRenderer> _spawnedConnections =
        new List<MapConnectionRenderer>();

    private readonly List<ConnectionData> _connectionDataList =
        new List<ConnectionData>();

    private MapNode[,] _nodeGrid;
    private MapLayoutCalculator _layoutCalculator;
    private IMapSelectionHandler _selectionHandler;
    private int _totalFloors;
    private int _gridWidth;
    private Coroutine _scrollCoroutine;

    #endregion

    #region Getters

    /// <summary>
    /// Gets whether the map has been initialized with data.
    /// </summary>
    public bool IsInitialized => _nodeGrid != null;

    #endregion

    #region Unity Message Methods

    private void Awake()
    {
        ValidateDependencies();
    }

    private void OnDestroy()
    {
        ClearMap();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the map view with node data and a selection handler.
    /// </summary>
    /// <param name="nodeGrid">The generated node grid from MapGenerator.</param>
    /// <param name="selectionHandler">Handler for node selection events.</param>
    public void Initialize(MapNode[,] nodeGrid, IMapSelectionHandler selectionHandler)
    {
        if (nodeGrid == null)
        {
            Debug.LogError("MapView: nodeGrid is null.", this);
            return;
        }

        ClearMap();

        _nodeGrid = nodeGrid;
        _selectionHandler = selectionHandler;
        _totalFloors = nodeGrid.GetLength(0);
        _gridWidth = nodeGrid.GetLength(1);

        _layoutCalculator = new MapLayoutCalculator(
            floorSpacing,
            columnSpacing,
            leftPadding,
            _gridWidth);

        SetupContentSize();
        SpawnNodes();
        SpawnConnections();
        RefreshNodeStates();
    }

    /// <summary>
    /// Refreshes the visual state of all nodes based on game state.
    /// Call this after the player moves or game state changes.
    /// </summary>
    public void RefreshNodeStates()
    {
        if (_selectionHandler == null)
        {
            return;
        }

        int currentFloor = _selectionHandler.GetCurrentFloor();

        foreach (KeyValuePair<(int floor, int column), MapNodeView> kvp in _spawnedNodes)
        {
            MapNodeView nodeView = kvp.Value;
            MapNode node = nodeView.MapNode;

            bool isAccessible = _selectionHandler.CanSelectNode(node);
            bool isVisited = _selectionHandler.IsNodeVisited(node);
            bool isCurrent = node.floor == currentFloor;

            nodeView.SetState(isAccessible, isVisited, isCurrent);
        }

        RefreshConnectionStates();
    }

    /// <summary>
    /// Scrolls the view to center on a specific floor.
    /// </summary>
    /// <param name="floor">The floor to scroll to.</param>
    /// <param name="animate">Whether to animate the scroll.</param>
    public void ScrollToFloor(int floor, bool animate = true)
    {
        if (scrollRect == null || _layoutCalculator == null)
        {
            return;
        }

        float viewportWidth = scrollRect.viewport.rect.width;
        float targetPosition = _layoutCalculator.CalculateScrollPositionForFloor(
            floor,
            _totalFloors,
            viewportWidth);

        if (animate)
        {
            AnimateScrollTo(targetPosition);
        }
        else
        {
            scrollRect.horizontalNormalizedPosition = targetPosition;
        }
    }

    /// <summary>
    /// Clears all spawned nodes and connections.
    /// </summary>
    public void ClearMap()
    {
        StopScrollAnimation();
        ClearConnections();
        ClearNodes();

        _nodeGrid = null;
        _layoutCalculator = null;
        _connectionDataList.Clear();
    }

    /// <summary>
    /// Called when the view is shown.
    /// </summary>
    public override void OnShow()
    {
        base.OnShow();

        if (_selectionHandler != null)
        {
            int currentFloor = _selectionHandler.GetCurrentFloor();
            ScrollToFloor(Mathf.Max(0, currentFloor), animate: false);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates that required dependencies are assigned.
    /// </summary>
    private void ValidateDependencies()
    {
        if (spriteConfig == null)
        {
            Debug.LogError("MapView: spriteConfig is not assigned.", this);
        }

        if (scrollRect == null)
        {
            Debug.LogError("MapView: scrollRect is not assigned.", this);
        }

        if (contentRoot == null)
        {
            Debug.LogError("MapView: contentRoot is not assigned.", this);
        }

        if (connectionRoot == null)
        {
            Debug.LogError("MapView: connectionRoot is not assigned.", this);
        }

        if (nodeViewPrefab == null)
        {
            Debug.LogError("MapView: nodeViewPrefab is not assigned.", this);
        }

        if (connectionPrefab == null)
        {
            Debug.LogError("MapView: connectionPrefab is not assigned.", this);
        }
    }

    /// <summary>
    /// Sets up the content area size based on the grid dimensions.
    /// </summary>
    private void SetupContentSize()
    {
        if (contentRoot == null || _layoutCalculator == null)
        {
            return;
        }

        float width = _layoutCalculator.CalculateContentWidth(_totalFloors);
        float height = _layoutCalculator.CalculateContentHeight();

        contentRoot.sizeDelta = new Vector2(width, height);
    }

    /// <summary>
    /// Spawns node views for all nodes in the grid.
    /// </summary>
    private void SpawnNodes()
    {
        for (int floor = 0; floor < _totalFloors; floor++)
        {
            for (int column = 0; column < _gridWidth; column++)
            {
                MapNode node = _nodeGrid[floor, column];
                if (node == null)
                {
                    continue;
                }

                SpawnNodeView(node);
            }
        }
    }

    /// <summary>
    /// Spawns a single node view.
    /// </summary>
    /// <param name="node">The node to spawn a view for.</param>
    private void SpawnNodeView(MapNode node)
    {
        MapNodeView nodeView = Instantiate(nodeViewPrefab, contentRoot);

        Vector2 position = _layoutCalculator.CalculateNodePosition(node.floor, node.column);
        RectTransform rectTransform = nodeView.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;

        Sprite nodeSprite = spriteConfig.GetSprite(node.nodeType);
        Color tintColor = spriteConfig.GetTintColor(node.nodeType);

        nodeView.Initialize(node, nodeSprite, tintColor, OnNodeClicked);

        if (spriteConfig.HighlightRingSprite != null)
        {
            nodeView.SetHighlightRingSprite(spriteConfig.HighlightRingSprite);
        }

        if (spriteConfig.VisitedIndicatorSprite != null)
        {
            nodeView.SetVisitedIndicatorSprite(spriteConfig.VisitedIndicatorSprite);
        }

        _spawnedNodes[(node.floor, node.column)] = nodeView;
    }

    /// <summary>
    /// Spawns connections between all connected nodes.
    /// </summary>
    private void SpawnConnections()
    {
        _connectionDataList.Clear();

        for (int floor = 0; floor < _totalFloors - 1; floor++)
        {
            for (int column = 0; column < _gridWidth; column++)
            {
                MapNode node = _nodeGrid[floor, column];
                if (node == null)
                {
                    continue;
                }

                foreach (int targetColumn in node.connections)
                {
                    SpawnConnection(node.floor, node.column, node.floor + 1, targetColumn);
                }
            }
        }
    }

    /// <summary>
    /// Spawns a single connection between two nodes.
    /// </summary>
    /// <param name="startFloor">Starting floor.</param>
    /// <param name="startColumn">Starting column.</param>
    /// <param name="endFloor">Ending floor.</param>
    /// <param name="endColumn">Ending column.</param>
    private void SpawnConnection(int startFloor, int startColumn, int endFloor, int endColumn)
    {
        MapConnectionRenderer connection = Instantiate(connectionPrefab, connectionRoot);

        Vector2 startPos2D = _layoutCalculator.CalculateNodePosition(startFloor, startColumn);
        Vector2 endPos2D = _layoutCalculator.CalculateNodePosition(endFloor, endColumn);

        Vector3 startWorldPos = GetWorldPositionFromAnchoredPosition(startPos2D);
        Vector3 endWorldPos = GetWorldPositionFromAnchoredPosition(endPos2D);

        connection.SetConnection(startWorldPos, endWorldPos, isAccessible: false, isVisited: false);

        _spawnedConnections.Add(connection);
        _connectionDataList.Add(new ConnectionData
        {
            startFloor = startFloor,
            startColumn = startColumn,
            endFloor = endFloor,
            endColumn = endColumn,
            renderer = connection
        });
    }

    /// <summary>
    /// Converts an anchored position to world position.
    /// </summary>
    /// <param name="anchoredPosition">The anchored position.</param>
    /// <returns>The world position.</returns>
    private Vector3 GetWorldPositionFromAnchoredPosition(Vector2 anchoredPosition)
    {
        Vector3 localPosition = new Vector3(anchoredPosition.x, anchoredPosition.y, 0f);
        return contentRoot.TransformPoint(localPosition);
    }

    /// <summary>
    /// Refreshes the visual state of all connections.
    /// </summary>
    private void RefreshConnectionStates()
    {
        if (_selectionHandler == null)
        {
            return;
        }

        foreach (ConnectionData data in _connectionDataList)
        {
            MapNode startNode = _nodeGrid[data.startFloor, data.startColumn];
            MapNode endNode = _nodeGrid[data.endFloor, data.endColumn];

            bool startVisited = _selectionHandler.IsNodeVisited(startNode);
            bool endVisited = _selectionHandler.IsNodeVisited(endNode);
            bool isVisited = startVisited && endVisited;

            bool endAccessible = _selectionHandler.CanSelectNode(endNode);

            data.renderer.UpdateVisited(isVisited);
            data.renderer.UpdateAccessibility(endAccessible);
        }
    }

    /// <summary>
    /// Called when a node is clicked.
    /// </summary>
    /// <param name="node">The clicked node.</param>
    private void OnNodeClicked(MapNode node)
    {
        _selectionHandler?.OnNodeSelected(node);
    }

    /// <summary>
    /// Clears all spawned nodes.
    /// </summary>
    private void ClearNodes()
    {
        foreach (MapNodeView nodeView in _spawnedNodes.Values)
        {
            if (nodeView != null)
            {
                Destroy(nodeView.gameObject);
            }
        }

        _spawnedNodes.Clear();
    }

    /// <summary>
    /// Clears all spawned connections.
    /// </summary>
    private void ClearConnections()
    {
        foreach (MapConnectionRenderer connection in _spawnedConnections)
        {
            if (connection != null)
            {
                Destroy(connection.gameObject);
            }
        }

        _spawnedConnections.Clear();
    }

    /// <summary>
    /// Animates the scroll position to a target value.
    /// </summary>
    /// <param name="targetPosition">The target normalized scroll position.</param>
    private void AnimateScrollTo(float targetPosition)
    {
        StopScrollAnimation();
        _scrollCoroutine = StartCoroutine(ScrollAnimationCoroutine(targetPosition));
    }

    /// <summary>
    /// Stops any ongoing scroll animation.
    /// </summary>
    private void StopScrollAnimation()
    {
        if (_scrollCoroutine != null)
        {
            StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine for smooth scroll animation.
    /// </summary>
    /// <param name="targetPosition">The target normalized scroll position.</param>
    /// <returns>Coroutine enumerator.</returns>
    private System.Collections.IEnumerator ScrollAnimationCoroutine(float targetPosition)
    {
        float startPosition = scrollRect.horizontalNormalizedPosition;
        float elapsed = 0f;

        while (elapsed < scrollAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / scrollAnimationDuration);
            float smoothT = t * t * (3f - 2f * t);

            scrollRect.horizontalNormalizedPosition = Mathf.Lerp(startPosition, targetPosition, smoothT);
            yield return null;
        }

        scrollRect.horizontalNormalizedPosition = targetPosition;
        _scrollCoroutine = null;
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Data structure for tracking connection information.
    /// </summary>
    private struct ConnectionData
    {
        public int startFloor;
        public int startColumn;
        public int endFloor;
        public int endColumn;
        public MapConnectionRenderer renderer;
    }

    #endregion
}
