using System;
using System.Collections.Generic;

public sealed class AbilityStatProvider : IAbilityStatProvider
{
    public Dictionary<AbilityStatKey, float> EvaluateStats(GTagSet tagSet, IEnumerable<AbilityStatEntry> baseStats, IEnumerable<AbilityModifier> activeModifiers)
    {
        var resolvedBase = baseStats ?? Array.Empty<AbilityStatEntry>();
        var resolvedModifiers = activeModifiers ?? Array.Empty<AbilityModifier>();
        var resolvedTags = tagSet ?? new GTagSet();

        return AbilityModifierRuntime.Evaluate(resolvedTags, resolvedBase, resolvedModifiers);
    }
}
