using System.Collections.Generic;

/// <summary>
/// Represents the type of encounter at a map node.
/// </summary>
public enum NodeType
{
    /// <summary>Standard monster encounter.</summary>
    Monster,
    /// <summary>Elite monster encounter with better rewards.</summary>
    Elite,
    /// <summary>Rest site for healing or upgrading.</summary>
    RestSite,
    /// <summary>Shop for purchasing items and cards.</summary>
    Shop,
    /// <summary>Treasure room with guaranteed rewards.</summary>
    Treasure,
    /// <summary>Random event encounter.</summary>
    Event,
    /// <summary>Boss encounter at the end of an act.</summary>
    Boss
}

/// <summary>
/// Represents a single node on the map grid.
/// </summary>
[System.Serializable]
public class MapNode
{
    #region Fields

    /// <summary>The floor (row) this node is on.</summary>
    public int floor;

    /// <summary>The column position of this node.</summary>
    public int column;

    /// <summary>The type of encounter at this node.</summary>
    public NodeType nodeType;

    /// <summary>List of column indices this node connects to on the next floor.</summary>
    public List<int> connections;

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a new map node at the specified position.
    /// </summary>
    /// <param name="floor">The floor (row) of the node.</param>
    /// <param name="column">The column position of the node.</param>
    /// <param name="nodeType">The type of encounter at this node.</param>
    public MapNode(int floor, int column, NodeType nodeType)
    {
        this.floor = floor;
        this.column = column;
        this.nodeType = nodeType;
        this.connections = new List<int>();
    }

    #endregion
}
