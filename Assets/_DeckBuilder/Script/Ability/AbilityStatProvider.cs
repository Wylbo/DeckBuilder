using System;
using System.Collections.Generic;

/// <summary>
/// Resolves ability stats by combining ability-defined entries (flat/ratio/copy) with caster globals,
/// then applies ability modifiers. Per-cast overrides are handled in AbilityExecutor.GetStat.
/// </summary>
public sealed class AbilityStatProvider : IAbilityStatProvider
{
    #region Fields
    #endregion

    #region Private Members
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    #endregion

    #region Public Methods
    public Dictionary<AbilityStatKey, float> EvaluateStats(
        GTagSet tagSet,
        IEnumerable<AbilityStatEntry> baseStats,
        IEnumerable<AbilityModifier> activeModifiers,
        IReadOnlyDictionary<GlobalStatKey, float> globalStats)
    {
        Dictionary<AbilityStatKey, float> resolvedBase = ResolveBaseStats(baseStats, globalStats);
        IEnumerable<AbilityModifier> resolvedModifiers = activeModifiers ?? Array.Empty<AbilityModifier>();
        GTagSet resolvedTags = tagSet ?? new GTagSet();

        return AbilityModifierRuntime.Evaluate(resolvedTags, resolvedBase, resolvedModifiers);
    }
    #endregion

    #region Private Methods
    private static Dictionary<AbilityStatKey, float> ResolveBaseStats(
        IEnumerable<AbilityStatEntry> baseStats,
        IReadOnlyDictionary<GlobalStatKey, float> globalStats)
    {
        Dictionary<AbilityStatKey, float> result = new Dictionary<AbilityStatKey, float>();
        if (baseStats == null)
        {
            return result;
        }

        foreach (AbilityStatEntry stat in baseStats)
        {
            float value = 0f;
            switch (stat.Source)
            {
                case AbilityStatSource.Flat:
                    value = stat.Value;
                    break;
                case AbilityStatSource.RatioToGlobal:
                    if (globalStats != null && globalStats.TryGetValue(stat.GlobalKey, out float globalVal))
                    {
                        value = stat.Value * globalVal;
                    }

                    break;
                case AbilityStatSource.CopyGlobal:
                    if (globalStats != null && globalStats.TryGetValue(stat.GlobalKey, out float copyVal))
                    {
                        value = copyVal;
                    }

                    break;
            }

            result[stat.Key] = value;
        }

        return result;
    }
    #endregion
}
