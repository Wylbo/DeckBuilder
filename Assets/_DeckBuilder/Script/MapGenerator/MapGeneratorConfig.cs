using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable weight configuration for a node type.
/// </summary>
[Serializable]
public class NodeTypeWeight
{
    #region Fields

    /// <summary>The type of node.</summary>
    public NodeType nodeType;

    /// <summary>The probability weight for this node type (0-100).</summary>
    [Range(0f, 100f)]
    [Tooltip("Probability weight for this node type (0-100)")]
    public float weight;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new node type weight configuration.
    /// </summary>
    /// <param name="nodeType">The node type.</param>
    /// <param name="weight">The probability weight.</param>
    public NodeTypeWeight(NodeType nodeType, float weight)
    {
        this.nodeType = nodeType;
        this.weight = weight;
    }

    #endregion
}

/// <summary>
/// Configuration asset for map generation parameters.
/// Based on Slay the Spire map generation algorithm.
/// </summary>
[CreateAssetMenu(fileName = "MapGeneratorConfig", menuName = "DeckBuilder/Map Generator Config")]
public class MapGeneratorConfig : ScriptableObject
{
    #region Fields

    [Header("Grid Settings")]
    [SerializeField]
    [Tooltip("Number of columns in the map grid")]
    [Range(3, 15)]
    private int gridWidth = 7;

    [SerializeField]
    [Tooltip("Total number of floors excluding boss floor")]
    [Range(5, 30)]
    private int totalFloors = 15;

    [SerializeField]
    [Tooltip("Number of paths to generate from bottom to top")]
    [Range(2, 10)]
    private int numberOfPaths = 6;

    [Header("Fixed Floor Assignments")]
    [SerializeField]
    [Tooltip("Floor index where all nodes are treasure rooms (0-indexed)")]
    private int treasureFloor = 8;

    [SerializeField]
    [Tooltip("Floor index where all nodes are rest sites before boss (0-indexed)")]
    private int preBossRestFloor = 14;

    [Header("Constraint Settings")]
    [SerializeField]
    [Tooltip("Minimum floor where Elite encounters can spawn (0-indexed)")]
    private int minEliteFloor = 5;

    [SerializeField]
    [Tooltip("Minimum floor where Rest Sites can spawn (0-indexed)")]
    private int minRestSiteFloor = 5;

    [SerializeField]
    [Tooltip("Rest Sites cannot spawn on the floor before the pre-boss rest floor")]
    private bool preventRestBeforePreBoss = true;

    [Header("Node Type Weights")]
    [SerializeField]
    [Tooltip("Probability weights for each node type")]
    private List<NodeTypeWeight> nodeTypeWeights = new()
    {
        new NodeTypeWeight(NodeType.Monster, 45f),
        new NodeTypeWeight(NodeType.Elite, 8f),
        new NodeTypeWeight(NodeType.Event, 15f),
        new NodeTypeWeight(NodeType.RestSite, 12f),
        new NodeTypeWeight(NodeType.Treasure, 0f),
        new NodeTypeWeight(NodeType.Shop, 5f)
    };

    [Header("Visualization")]
    [SerializeField]
    [Tooltip("Horizontal spacing between nodes in gizmo view")]
    private float horizontalSpacing = 2f;

    [SerializeField]
    [Tooltip("Vertical spacing between floors in gizmo view")]
    private float verticalSpacing = 3f;

    [SerializeField]
    [Tooltip("Size of node spheres in gizmo view")]
    private float nodeSize = 0.4f;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the width of the map grid (number of columns).
    /// </summary>
    public int GridWidth => gridWidth;

    /// <summary>
    /// Gets the total number of floors excluding the boss floor.
    /// </summary>
    public int TotalFloors => totalFloors;

    /// <summary>
    /// Gets the number of paths to generate.
    /// </summary>
    public int NumberOfPaths => numberOfPaths;

    /// <summary>
    /// Gets the floor index where all nodes are treasure rooms.
    /// </summary>
    public int TreasureFloor => treasureFloor;

    /// <summary>
    /// Gets the floor index for pre-boss rest sites.
    /// </summary>
    public int PreBossRestFloor => preBossRestFloor;

    /// <summary>
    /// Gets the minimum floor where Elite encounters can spawn.
    /// </summary>
    public int MinEliteFloor => minEliteFloor;

    /// <summary>
    /// Gets the minimum floor where Rest Sites can spawn.
    /// </summary>
    public int MinRestSiteFloor => minRestSiteFloor;

    /// <summary>
    /// Gets whether Rest Sites are prevented on the floor before pre-boss.
    /// </summary>
    public bool PreventRestBeforePreBoss => preventRestBeforePreBoss;

    /// <summary>
    /// Gets the node type weights for random selection.
    /// </summary>
    public List<NodeTypeWeight> NodeTypeWeights => nodeTypeWeights;

    /// <summary>
    /// Gets the horizontal spacing for visualization.
    /// </summary>
    public float HorizontalSpacing => horizontalSpacing;

    /// <summary>
    /// Gets the vertical spacing for visualization.
    /// </summary>
    public float VerticalSpacing => verticalSpacing;

    /// <summary>
    /// Gets the node size for visualization.
    /// </summary>
    public float NodeSize => nodeSize;

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the weight for a specific node type.
    /// </summary>
    /// <param name="nodeType">The node type to look up.</param>
    /// <returns>The weight for the node type, or 0 if not found.</returns>
    public float GetWeightForType(NodeType nodeType)
    {
        foreach (NodeTypeWeight weight in nodeTypeWeights)
        {
            if (weight.nodeType == nodeType)
            {
                return weight.weight;
            }
        }
        return 0f;
    }

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    /// <returns>True if configuration is valid.</returns>
    public bool Validate()
    {
        bool isValid = true;

        if (treasureFloor >= totalFloors)
        {
            Debug.LogError("MapGeneratorConfig: Treasure floor must be less than total floors.");
            isValid = false;
        }

        if (preBossRestFloor >= totalFloors)
        {
            Debug.LogError("MapGeneratorConfig: Pre-boss rest floor must be less than total floors.");
            isValid = false;
        }

        if (numberOfPaths > gridWidth)
        {
            Debug.LogError("MapGeneratorConfig: Number of paths cannot exceed grid width.");
            isValid = false;
        }

        return isValid;
    }

    #endregion
}
