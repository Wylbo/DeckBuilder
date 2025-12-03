using System;
using System.Collections.Generic;

/// <summary>
/// Resolves ability stats by combining ability-defined entries (flat/ratio/copy) with caster globals,
/// then applies ability modifiers. Per-cast overrides are handled in AbilityExecutor.GetStat.
/// </summary>
public sealed class AbilityStatProvider : IAbilityStatProvider
{
    public Dictionary<AbilityStatKey, float> EvaluateStats(
        GTagSet tagSet,
        IEnumerable<AbilityStatEntry> baseStats,
        IEnumerable<AbilityModifier> activeModifiers,
        IReadOnlyDictionary<GlobalStatKey, float> globalStats)
    {
        var resolvedBase = ResolveBaseStats(baseStats, globalStats);
        var resolvedModifiers = activeModifiers ?? Array.Empty<AbilityModifier>();
        var resolvedTags = tagSet ?? new GTagSet();

        return AbilityModifierRuntime.Evaluate(resolvedTags, resolvedBase, resolvedModifiers);
    }

    private static Dictionary<AbilityStatKey, float> ResolveBaseStats(IEnumerable<AbilityStatEntry> baseStats, IReadOnlyDictionary<GlobalStatKey, float> globalStats)
    {
        var result = new Dictionary<AbilityStatKey, float>();
        if (baseStats == null)
            return result;

        foreach (var stat in baseStats)
        {
            float value = 0f;
            switch (stat.Source)
            {
                case AbilityStatSource.Flat:
                    value = stat.Value;
                    break;
                case AbilityStatSource.RatioToGlobal:
                    if (globalStats != null && globalStats.TryGetValue(stat.GlobalKey, out float globalVal))
                        value = stat.Value * globalVal;
                    break;
                case AbilityStatSource.CopyGlobal:
                    if (globalStats != null && globalStats.TryGetValue(stat.GlobalKey, out float copyVal))
                        value = copyVal;
                    break;
            }

            result[stat.Key] = value;
        }

        return result;
    }
}
