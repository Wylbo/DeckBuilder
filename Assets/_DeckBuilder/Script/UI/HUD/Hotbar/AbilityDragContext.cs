using UnityEngine;

/// <summary>
/// Minimal shared state for dragging abilities from the inventory into the hotbar.
/// Keeps track of the payload and an optional preview visual.
/// </summary>
public static class AbilityDragContext
{
    private static AbilityDragPreview preview;

    public static Ability DraggedAbility { get; private set; }
    public static bool HasPayload => DraggedAbility != null;

    public static void RegisterPreview(AbilityDragPreview previewInstance)
    {
        preview = previewInstance;
    }

    public static void BeginDrag(Ability ability)
    {
        TooltipManager.Instance?.HideImmediate();
        DraggedAbility = ability;
        preview?.Show(ability?.Icon);
    }

    public static void UpdatePosition(Vector2 screenPosition)
    {
        preview?.Move(screenPosition);
    }

    public static void EndDrag()
    {
        preview?.Hide();
        DraggedAbility = null;
    }
}
