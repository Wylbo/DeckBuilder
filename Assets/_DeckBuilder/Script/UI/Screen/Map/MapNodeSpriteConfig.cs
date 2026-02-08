using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable mapping between a NodeType and its display sprite.
/// </summary>
[Serializable]
public class NodeTypeSprite
{
    #region Fields

    /// <summary>The type of node this sprite represents.</summary>
    public NodeType nodeType;

    /// <summary>The sprite to display for this node type.</summary>
    public Sprite sprite;

    /// <summary>Optional tint color for the sprite.</summary>
    public Color tintColor = Color.white;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new NodeTypeSprite mapping.
    /// </summary>
    /// <param name="nodeType">The node type.</param>
    /// <param name="sprite">The sprite to use.</param>
    public NodeTypeSprite(NodeType nodeType, Sprite sprite)
    {
        this.nodeType = nodeType;
        this.sprite = sprite;
        this.tintColor = Color.white;
    }

    #endregion
}

/// <summary>
/// ScriptableObject containing sprite mappings for each map node type.
/// Allows designers to configure visual appearance of nodes without code changes.
/// </summary>
[CreateAssetMenu(fileName = "MapNodeSpriteConfig", menuName = "DeckBuilder/Map Node Sprite Config")]
public class MapNodeSpriteConfig : ScriptableObject
{
    #region Fields

    [SerializeField]
    [Tooltip("Sprite mappings for each node type")]
    private List<NodeTypeSprite> nodeTypeSprites = new List<NodeTypeSprite>();

    [SerializeField]
    [Tooltip("Default sprite used when no mapping is found")]
    private Sprite defaultSprite;

    [SerializeField]
    [Tooltip("Sprite for the highlight ring around selected/current nodes")]
    private Sprite highlightRingSprite;

    [SerializeField]
    [Tooltip("Sprite indicating a node has been visited")]
    private Sprite visitedIndicatorSprite;

    #endregion

    #region Getters

    /// <summary>
    /// Gets the highlight ring sprite.
    /// </summary>
    public Sprite HighlightRingSprite => highlightRingSprite;

    /// <summary>
    /// Gets the visited indicator sprite.
    /// </summary>
    public Sprite VisitedIndicatorSprite => visitedIndicatorSprite;

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the sprite for a specific node type.
    /// </summary>
    /// <param name="nodeType">The node type to get the sprite for.</param>
    /// <returns>The sprite for the node type, or the default sprite if not found.</returns>
    public Sprite GetSprite(NodeType nodeType)
    {
        foreach (NodeTypeSprite mapping in nodeTypeSprites)
        {
            if (mapping.nodeType == nodeType)
            {
                return mapping.sprite != null ? mapping.sprite : defaultSprite;
            }
        }

        return defaultSprite;
    }

    /// <summary>
    /// Gets the tint color for a specific node type.
    /// </summary>
    /// <param name="nodeType">The node type to get the color for.</param>
    /// <returns>The tint color for the node type.</returns>
    public Color GetTintColor(NodeType nodeType)
    {
        foreach (NodeTypeSprite mapping in nodeTypeSprites)
        {
            if (mapping.nodeType == nodeType)
            {
                return mapping.tintColor;
            }
        }

        return Color.white;
    }

    /// <summary>
    /// Validates that all node types have sprite mappings.
    /// </summary>
    /// <returns>True if all node types have valid sprites.</returns>
    public bool Validate()
    {
        bool isValid = true;

        foreach (NodeType nodeType in Enum.GetValues(typeof(NodeType)))
        {
            Sprite sprite = GetSprite(nodeType);
            if (sprite == null)
            {
                Debug.LogWarning($"MapNodeSpriteConfig: No sprite found for NodeType.{nodeType}", this);
                isValid = false;
            }
        }

        return isValid;
    }

    #endregion
}
