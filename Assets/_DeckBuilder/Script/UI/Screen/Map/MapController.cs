using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Controller that bridges the MapGenerator and MapView.
/// Manages map state, tracks player progress, and handles node selection.
/// </summary>
public class MapController : MonoBehaviour, IMapSelectionHandler
{
    #region Fields

    [SerializeField]
    [Tooltip("Reference to the map generator")]
    private MapGenerator mapGenerator;

    [SerializeField]
    [Tooltip("Reference to the UI manager for showing the map view")]
    private UIManager uiManager;

    #endregion

    #region Private Members

    private MapNode[,] _nodeGrid;
    private int _currentFloor = -1;
    private int _currentColumn = -1;
    private readonly HashSet<(int floor, int column)> _visitedNodes = new HashSet<(int floor, int column)>();
    private MapView _mapView;

    #endregion

    #region Events

    /// <summary>
    /// Event fired when a node is selected by the player.
    /// </summary>
    public event Action<MapNode> OnNodeSelectedEvent;

    /// <summary>
    /// Event fired when the player reaches the boss node.
    /// </summary>
    public event Action OnBossReached;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the current floor the player is on. Returns -1 if not started.
    /// </summary>
    public int CurrentFloor => _currentFloor;

    /// <summary>
    /// Gets the current column the player is in. Returns -1 if not started.
    /// </summary>
    public int CurrentColumn => _currentColumn;

    /// <summary>
    /// Gets whether the map has been generated.
    /// </summary>
    public bool IsMapGenerated => _nodeGrid != null;

    /// <summary>
    /// Gets the generated node grid.
    /// </summary>
    public MapNode[,] NodeGrid => _nodeGrid;

    #endregion

    #region Unity Message Methods

    private void Awake()
    {
        ValidateDependencies();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates a new map and shows the map view.
    /// </summary>
    [Button]
    public void GenerateAndShowMap()
    {
        GenerateMap();
        ShowMap();
    }

    /// <summary>
    /// Generates a new map without showing the view.
    /// Resets player progress.
    /// </summary>
    public void GenerateMap()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapController: mapGenerator is not assigned.", this);
            return;
        }

        _nodeGrid = mapGenerator.GenerateMap();
        ResetProgress();
    }

    /// <summary>
    /// Shows the map view with the current map data.
    /// </summary>
    public void ShowMap()
    {
        if (_nodeGrid == null)
        {
            Debug.LogError("MapController: No map has been generated. Call GenerateMap first.", this);
            return;
        }

        if (uiManager == null)
        {
            Debug.LogError("MapController: uiManager is not assigned.", this);
            return;
        }

        _mapView = uiManager.Show<MapView>();

        if (_mapView != null)
        {
            _mapView.Initialize(_nodeGrid, this);
        }
    }

    /// <summary>
    /// Hides the map view.
    /// </summary>
    public void HideMap()
    {
        if (uiManager != null)
        {
            uiManager.Hide<MapView>();
        }

        _mapView = null;
    }

    /// <summary>
    /// Refreshes the map view to reflect current state.
    /// Call this after external state changes.
    /// </summary>
    public void RefreshMapView()
    {
        if (_mapView != null && _mapView.IsVisible)
        {
            _mapView.RefreshNodeStates();
        }
    }

    /// <summary>
    /// Resets the player's progress on the map.
    /// </summary>
    public void ResetProgress()
    {
        _currentFloor = -1;
        _currentColumn = -1;
        _visitedNodes.Clear();
    }

    /// <summary>
    /// Marks a node as visited without triggering selection events.
    /// Useful for loading saved progress.
    /// </summary>
    /// <param name="floor">The floor of the node.</param>
    /// <param name="column">The column of the node.</param>
    public void MarkNodeVisited(int floor, int column)
    {
        _visitedNodes.Add((floor, column));
        _currentFloor = floor;
        _currentColumn = column;
    }

    /// <summary>
    /// Gets the node at the specified position.
    /// </summary>
    /// <param name="floor">The floor index.</param>
    /// <param name="column">The column index.</param>
    /// <returns>The node at the position, or null if invalid.</returns>
    public MapNode GetNode(int floor, int column)
    {
        if (_nodeGrid == null)
        {
            return null;
        }

        if (floor < 0 || floor >= _nodeGrid.GetLength(0))
        {
            return null;
        }

        if (column < 0 || column >= _nodeGrid.GetLength(1))
        {
            return null;
        }

        return _nodeGrid[floor, column];
    }

    /// <summary>
    /// Gets all accessible nodes from the current position.
    /// </summary>
    /// <returns>List of accessible nodes.</returns>
    public List<MapNode> GetAccessibleNodes()
    {
        List<MapNode> accessibleNodes = new List<MapNode>();

        if (_nodeGrid == null)
        {
            return accessibleNodes;
        }

        int gridWidth = _nodeGrid.GetLength(1);
        int totalFloors = _nodeGrid.GetLength(0);

        if (_currentFloor == -1)
        {
            for (int column = 0; column < gridWidth; column++)
            {
                MapNode node = _nodeGrid[0, column];
                if (node != null)
                {
                    accessibleNodes.Add(node);
                }
            }
        }
        else if (_currentFloor < totalFloors - 1)
        {
            MapNode currentNode = _nodeGrid[_currentFloor, _currentColumn];
            if (currentNode != null)
            {
                foreach (int targetColumn in currentNode.connections)
                {
                    MapNode nextNode = _nodeGrid[_currentFloor + 1, targetColumn];
                    if (nextNode != null)
                    {
                        accessibleNodes.Add(nextNode);
                    }
                }
            }
        }

        return accessibleNodes;
    }

    #endregion

    #region IMapSelectionHandler Implementation

    /// <summary>
    /// Called when the player selects a map node.
    /// </summary>
    /// <param name="node">The selected node.</param>
    public void OnNodeSelected(MapNode node)
    {
        if (node == null)
        {
            return;
        }

        if (!CanSelectNode(node))
        {
            Debug.LogWarning($"MapController: Cannot select node at floor {node.floor}, column {node.column}.", this);
            return;
        }

        _visitedNodes.Add((node.floor, node.column));
        _currentFloor = node.floor;
        _currentColumn = node.column;

        RefreshMapView();

        OnNodeSelectedEvent?.Invoke(node);

        if (node.nodeType == NodeType.Boss)
        {
            OnBossReached?.Invoke();
        }
    }

    /// <summary>
    /// Determines if a node can be selected based on current game state.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is selectable.</returns>
    public bool CanSelectNode(MapNode node)
    {
        if (node == null)
        {
            return false;
        }

        if (IsNodeVisited(node))
        {
            return false;
        }

        if (_currentFloor == -1)
        {
            return node.floor == 0;
        }

        if (node.floor != _currentFloor + 1)
        {
            return false;
        }

        MapNode currentNode = _nodeGrid[_currentFloor, _currentColumn];
        if (currentNode == null)
        {
            return false;
        }

        return currentNode.connections.Contains(node.column);
    }

    /// <summary>
    /// Determines if a node has been visited by the player.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node has been visited.</returns>
    public bool IsNodeVisited(MapNode node)
    {
        if (node == null)
        {
            return false;
        }

        return _visitedNodes.Contains((node.floor, node.column));
    }

    /// <summary>
    /// Gets the current floor the player is on.
    /// </summary>
    /// <returns>The current floor index, or -1 if not started.</returns>
    public int GetCurrentFloor()
    {
        return _currentFloor;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates that required dependencies are assigned.
    /// </summary>
    private void ValidateDependencies()
    {
        if (mapGenerator == null)
        {
            Debug.LogError("MapController: mapGenerator is not assigned.", this);
        }

        if (uiManager == null)
        {
            Debug.LogError("MapController: uiManager is not assigned.", this);
        }
    }

    #endregion
}
