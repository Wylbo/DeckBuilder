using UnityEngine;

/// <summary>
/// Implemented by UI elements that can provide tooltip data.
/// </summary>
public interface ITooltipSource
{
    RectTransform TooltipAnchor { get; }
    bool TryGetTooltipData(out TooltipData data);
}
