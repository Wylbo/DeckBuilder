using System.Collections.Generic;
using UnityEngine;

public interface IGlobalStatSource
{
    Dictionary<GlobalStatKey, float> EvaluateGlobalStats();
    Dictionary<GlobalStatKey, float> EvaluateGlobalStatsRaw();
}

[DisallowMultipleComponent]
public sealed class GlobalStatSource : MonoBehaviour, IGlobalStatSource
{
    [SerializeField] private List<GlobalStatEntry> baseStats = new List<GlobalStatEntry>();
    [SerializeField] private StatsModifierManager modifierProvider;

    public IReadOnlyList<GlobalStatEntry> BaseStats => baseStats;

    public Dictionary<GlobalStatKey, float> EvaluateGlobalStats()
    {
        return GlobalModifierRuntime.Evaluate(baseStats, modifierProvider?.ActiveGlobalModifiers);
    }

    public Dictionary<GlobalStatKey, float> EvaluateGlobalStatsRaw()
    {
        var result = new Dictionary<GlobalStatKey, float>();
        if (baseStats == null)
            return result;

        foreach (var entry in baseStats)
            result[entry.Key] = entry.Value;

        return result;
    }


    public float GetGlobalStat(GlobalStatKey key)
    {
        var stats = EvaluateGlobalStats();
        return stats != null && stats.TryGetValue(key, out var value) ? value : 0f;
    }

    public float GetGlobalStatRaw(GlobalStatKey key)
    {
        var stats = EvaluateGlobalStatsRaw();
        return stats != null && stats.TryGetValue(key, out var value) ? value : 0f;
    }
}
