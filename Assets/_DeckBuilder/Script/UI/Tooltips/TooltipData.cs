using System;
using UnityEngine;

/// <summary>
/// Shared tooltip payload used by hoverable UI elements.
/// </summary>
public struct TooltipData
{
    public string Title;
    public string Description;
    public Sprite Icon;

    public TooltipData(string title, string description, Sprite icon = null)
    {
        Title = title;
        Description = description;
        Icon = icon;
    }

    public bool HasContent =>
        Icon != null ||
        !string.IsNullOrWhiteSpace(Title) ||
        !string.IsNullOrWhiteSpace(Description);

    public static TooltipData FromAbility(Ability ability)
    {
        if (ability == null)
            return default;

        return new TooltipData(ability.name, ability.Tooltip, ability.Icon);
    }
}
