/// <summary>
/// Interface for handling map node selection events.
/// Implemented by game controllers to respond to player node choices.
/// </summary>
public interface IMapSelectionHandler
{
    /// <summary>
    /// Called when the player selects a map node.
    /// </summary>
    /// <param name="node">The selected map node.</param>
    void OnNodeSelected(MapNode node);

    /// <summary>
    /// Determines if a node can be selected based on current game state.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node is selectable.</returns>
    bool CanSelectNode(MapNode node);

    /// <summary>
    /// Determines if a node has been visited by the player.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if the node has been visited.</returns>
    bool IsNodeVisited(MapNode node);

    /// <summary>
    /// Gets the current floor the player is on.
    /// Returns -1 if the player hasn't started yet.
    /// </summary>
    /// <returns>The current floor index.</returns>
    int GetCurrentFloor();
}
