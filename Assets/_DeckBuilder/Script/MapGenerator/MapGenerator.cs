using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generates a roguelike map structure based on Slay the Spire's algorithm.
/// Creates a grid of nodes with paths that connect from bottom to top without crossing.
/// </summary>
public class MapGenerator : MonoBehaviour
{
    #region Fields

    [SerializeField]
    [Tooltip("Configuration asset containing all map generation parameters")]
    [Required]
    private MapGeneratorConfig config;

    [SerializeField]
    [Tooltip("Random seed for reproducible generation (0 for random)")]
    private int seed = 0;

    #endregion

    #region Private Members
    private MapNode[,] _nodeGrid;

    private List<List<int>> _paths;
    private System.Random _random;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the generated node grid.
    /// </summary>
    public MapNode[,] NodeGrid => _nodeGrid;

    /// <summary>
    /// Gets the generated paths.
    /// </summary>
    public List<List<int>> Paths => _paths;

    #endregion

    #region Unity Message Methods

    private void Awake()
    {
        ValidateDependencies();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates a new map using the configured settings.
    /// </summary>
    /// <returns>The generated node grid.</returns>
    [Button("Generate Map")]
    public MapNode[,] GenerateMap()
    {
        if (!ValidateDependencies())
        {
            return null;
        }

        InitializeRandom();
        InitializeGrid();
        GeneratePaths();
        CreateConnectionsFromPaths();
        AssignNodeTypes();
        AddBossNode();

        return _nodeGrid;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates that all required dependencies are assigned.
    /// </summary>
    /// <returns>True if all dependencies are valid.</returns>
    private bool ValidateDependencies()
    {
        if (config == null)
        {
            Debug.LogError("MapGenerator: Configuration asset is not assigned.", this);
            return false;
        }

        return config.Validate();
    }

    /// <summary>
    /// Initializes the random number generator with the configured seed.
    /// </summary>
    private void InitializeRandom()
    {
        int effectiveSeed = seed == 0 ? Environment.TickCount : seed;
        _random = new System.Random(effectiveSeed);
    }

    /// <summary>
    /// Initializes an empty node grid.
    /// </summary>
    private void InitializeGrid()
    {
        _nodeGrid = new MapNode[config.TotalFloors + 1, config.GridWidth];
        _paths = new List<List<int>>();
    }

    /// <summary>
    /// Generates paths from floor 0 to the top floor.
    /// Paths cannot cross each other.
    /// </summary>
    private void GeneratePaths()
    {
        List<int> startingColumns = GenerateStartingColumns();

        foreach (int startColumn in startingColumns)
        {
            List<int> path = GenerateSinglePath(startColumn);
            _paths.Add(path);
        }
    }

    /// <summary>
    /// Generates unique starting column positions for all paths.
    /// </summary>
    /// <returns>List of starting column indices.</returns>
    private List<int> GenerateStartingColumns()
    {
        List<int> availableColumns = new List<int>();
        for (int i = 0; i < config.GridWidth; i++)
        {
            availableColumns.Add(i);
        }

        List<int> startingColumns = new List<int>();
        int pathCount = Mathf.Min(config.NumberOfPaths, config.GridWidth);

        for (int i = 0; i < pathCount; i++)
        {
            int randomIndex = _random.Next(availableColumns.Count);
            startingColumns.Add(availableColumns[randomIndex]);
            availableColumns.RemoveAt(randomIndex);
        }

        startingColumns.Sort();
        return startingColumns;
    }

    /// <summary>
    /// Generates a single path from the starting column to the top floor.
    /// </summary>
    /// <param name="startColumn">The starting column index.</param>
    /// <returns>List of column indices for each floor.</returns>
    private List<int> GenerateSinglePath(int startColumn)
    {
        List<int> path = new List<int> { startColumn };

        for (int floor = 1; floor < config.TotalFloors; floor++)
        {
            int currentColumn = path[floor - 1];
            int nextColumn = ChooseNextColumn(currentColumn, floor);
            path.Add(nextColumn);
        }

        return path;
    }

    /// <summary>
    /// Chooses the next column for a path, ensuring no crossing with existing paths.
    /// </summary>
    /// <param name="currentColumn">The current column position.</param>
    /// <param name="floor">The floor being generated.</param>
    /// <returns>The chosen next column index.</returns>
    private int ChooseNextColumn(int currentColumn, int floor)
    {
        List<int> possibleColumns = GetPossibleNextColumns(currentColumn);
        RemoveCrossingColumns(possibleColumns, currentColumn, floor);

        if (possibleColumns.Count == 0)
        {
            return currentColumn;
        }

        int randomIndex = _random.Next(possibleColumns.Count);
        return possibleColumns[randomIndex];
    }

    /// <summary>
    /// Gets all possible next column positions (current, left, or right).
    /// </summary>
    /// <param name="currentColumn">The current column position.</param>
    /// <returns>List of possible column indices.</returns>
    private List<int> GetPossibleNextColumns(int currentColumn)
    {
        List<int> possible = new List<int>();

        if (currentColumn > 0)
        {
            possible.Add(currentColumn - 1);
        }

        possible.Add(currentColumn);

        if (currentColumn < config.GridWidth - 1)
        {
            possible.Add(currentColumn + 1);
        }

        return possible;
    }

    /// <summary>
    /// Removes columns that would cause path crossing.
    /// </summary>
    /// <param name="possibleColumns">List of possible columns to filter.</param>
    /// <param name="currentColumn">The current column position.</param>
    /// <param name="floor">The floor being generated.</param>
    private void RemoveCrossingColumns(List<int> possibleColumns, int currentColumn, int floor)
    {
        for (int i = possibleColumns.Count - 1; i >= 0; i--)
        {
            int targetColumn = possibleColumns[i];

            if (WouldCrossExistingPath(currentColumn, targetColumn, floor))
            {
                possibleColumns.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Checks if moving from current to target column would cross an existing path.
    /// </summary>
    /// <param name="currentColumn">The current column.</param>
    /// <param name="targetColumn">The target column.</param>
    /// <param name="floor">The floor being generated.</param>
    /// <returns>True if the move would cause a crossing.</returns>
    private bool WouldCrossExistingPath(int currentColumn, int targetColumn, int floor)
    {
        foreach (List<int> existingPath in _paths)
        {
            if (existingPath.Count <= floor)
            {
                continue;
            }

            int existingCurrent = existingPath[floor - 1];
            int existingTarget = existingPath[floor];

            if (PathsWouldCross(currentColumn, targetColumn, existingCurrent, existingTarget))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if two path segments would cross each other.
    /// </summary>
    /// <param name="col1Start">Start column of first path.</param>
    /// <param name="col1End">End column of first path.</param>
    /// <param name="col2Start">Start column of second path.</param>
    /// <param name="col2End">End column of second path.</param>
    /// <returns>True if paths would cross.</returns>
    private bool PathsWouldCross(int col1Start, int col1End, int col2Start, int col2End)
    {
        if (col1Start == col2Start && col1End == col2End)
        {
            return false;
        }

        bool cross1 = col1Start < col2Start && col1End > col2End;
        bool cross2 = col1Start > col2Start && col1End < col2End;

        return cross1 || cross2;
    }

    /// <summary>
    /// Creates node connections based on the generated paths.
    /// </summary>
    private void CreateConnectionsFromPaths()
    {
        foreach (List<int> path in _paths)
        {
            for (int floor = 0; floor < path.Count; floor++)
            {
                int column = path[floor];
                EnsureNodeExists(floor, column);

                if (floor < path.Count - 1)
                {
                    int nextColumn = path[floor + 1];
                    AddConnectionIfNotExists(_nodeGrid[floor, column], nextColumn);
                }
            }
        }
    }

    /// <summary>
    /// Ensures a node exists at the specified position.
    /// </summary>
    /// <param name="floor">The floor index.</param>
    /// <param name="column">The column index.</param>
    private void EnsureNodeExists(int floor, int column)
    {
        if (_nodeGrid[floor, column] == null)
        {
            _nodeGrid[floor, column] = new MapNode(floor, column, NodeType.Monster);
        }
    }

    /// <summary>
    /// Adds a connection to a node if it doesn't already exist.
    /// </summary>
    /// <param name="node">The node to add the connection to.</param>
    /// <param name="targetColumn">The target column index.</param>
    private void AddConnectionIfNotExists(MapNode node, int targetColumn)
    {
        if (!node.connections.Contains(targetColumn))
        {
            node.connections.Add(targetColumn);
        }
    }

    /// <summary>
    /// Assigns node types based on floor rules and weighted random selection.
    /// </summary>
    private void AssignNodeTypes()
    {
        for (int floor = 0; floor < config.TotalFloors; floor++)
        {
            for (int column = 0; column < config.GridWidth; column++)
            {
                MapNode node = _nodeGrid[floor, column];
                if (node == null)
                {
                    continue;
                }

                NodeType nodeType = DetermineNodeType(node, floor);
                node.nodeType = nodeType;
            }
        }
    }

    /// <summary>
    /// Determines the node type based on floor rules and constraints.
    /// </summary>
    /// <param name="node">The node to determine type for.</param>
    /// <param name="floor">The floor index.</param>
    /// <returns>The determined node type.</returns>
    private NodeType DetermineNodeType(MapNode node, int floor)
    {
        if (floor == 0)
        {
            return NodeType.Monster;
        }

        if (floor == config.TreasureFloor)
        {
            return NodeType.Treasure;
        }

        if (floor == config.PreBossRestFloor)
        {
            return NodeType.RestSite;
        }

        return SelectWeightedNodeType(floor, node.column);
    }

    /// <summary>
    /// Selects a node type using weighted random selection with constraints.
    /// </summary>
    /// <param name="floor">The current floor.</param>
    /// <param name="column">The current column.</param>
    /// <returns>The selected node type.</returns>
    private NodeType SelectWeightedNodeType(int floor, int column)
    {
        Dictionary<NodeType, float> weights = BuildConstrainedWeights(floor, column);
        return WeightedRandomSelection(weights);
    }

    /// <summary>
    /// Builds the weight dictionary with constraints applied.
    /// </summary>
    /// <param name="floor">The current floor.</param>
    /// <param name="column">The current column.</param>
    /// <returns>Dictionary of allowed node types and their weights.</returns>
    private Dictionary<NodeType, float> BuildConstrainedWeights(int floor, int column)
    {
        Dictionary<NodeType, float> weights = new Dictionary<NodeType, float>();

        foreach (NodeTypeWeight nodeWeight in config.NodeTypeWeights)
        {
            if (IsNodeTypeAllowed(nodeWeight.nodeType, floor, column))
            {
                weights[nodeWeight.nodeType] = nodeWeight.weight;
            }
        }

        if (weights.Count == 0)
        {
            weights[NodeType.Monster] = 1f;
        }

        return weights;
    }

    /// <summary>
    /// Checks if a node type is allowed at the specified position.
    /// </summary>
    /// <param name="nodeType">The node type to check.</param>
    /// <param name="floor">The floor index.</param>
    /// <param name="column">The column index.</param>
    /// <returns>True if the node type is allowed.</returns>
    private bool IsNodeTypeAllowed(NodeType nodeType, int floor, int column)
    {
        if (nodeType == NodeType.Boss)
        {
            return false;
        }

        if (nodeType == NodeType.Elite && floor < config.MinEliteFloor)
        {
            return false;
        }

        if (nodeType == NodeType.RestSite)
        {
            if (floor < config.MinRestSiteFloor)
            {
                return false;
            }

            if (config.PreventRestBeforePreBoss && floor == config.PreBossRestFloor - 1)
            {
                return false;
            }
        }

        if (HasConsecutiveTypeOnPath(nodeType, floor, column))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if placing this node type would create consecutive special rooms on any path.
    /// Elite, Shop, and RestSite cannot be consecutive.
    /// </summary>
    /// <param name="nodeType">The node type to check.</param>
    /// <param name="floor">The floor index.</param>
    /// <param name="column">The column index.</param>
    /// <returns>True if this would create a consecutive special room.</returns>
    private bool HasConsecutiveTypeOnPath(NodeType nodeType, int floor, int column)
    {
        if (nodeType != NodeType.Elite && nodeType != NodeType.Shop && nodeType != NodeType.RestSite)
        {
            return false;
        }

        if (floor == 0)
        {
            return false;
        }

        List<NodeType> restrictedTypes = new List<NodeType> { NodeType.Elite, NodeType.Shop, NodeType.RestSite };

        for (int prevColumn = 0; prevColumn < config.GridWidth; prevColumn++)
        {
            MapNode prevNode = _nodeGrid[floor - 1, prevColumn];
            if (prevNode == null)
            {
                continue;
            }

            if (!prevNode.connections.Contains(column))
            {
                continue;
            }

            if (restrictedTypes.Contains(prevNode.nodeType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs weighted random selection from a dictionary of options.
    /// </summary>
    /// <param name="weights">Dictionary of options and their weights.</param>
    /// <returns>The selected node type.</returns>
    private NodeType WeightedRandomSelection(Dictionary<NodeType, float> weights)
    {
        float totalWeight = 0f;
        foreach (float weight in weights.Values)
        {
            totalWeight += weight;
        }

        float randomValue = (float)_random.NextDouble() * totalWeight;

        foreach (KeyValuePair<NodeType, float> pair in weights)
        {
            randomValue -= pair.Value;
            if (randomValue <= 0f)
            {
                return pair.Key;
            }
        }

        return NodeType.Monster;
    }

    /// <summary>
    /// Adds the boss node at the final floor.
    /// </summary>
    private void AddBossNode()
    {
        int bossFloor = config.TotalFloors;
        int bossColumn = config.GridWidth / 2;

        _nodeGrid[bossFloor, bossColumn] = new MapNode(bossFloor, bossColumn, NodeType.Boss);

        int preBossFloor = config.TotalFloors - 1;
        for (int column = 0; column < config.GridWidth; column++)
        {
            MapNode node = _nodeGrid[preBossFloor, column];
            if (node != null)
            {
                node.connections.Clear();
                node.connections.Add(bossColumn);
            }
        }
    }
    #endregion
}
