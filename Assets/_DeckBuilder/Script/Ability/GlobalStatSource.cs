using System.Collections.Generic;
using UnityEngine;

public interface IGlobalStatSource
{
    Dictionary<GlobalStatKey, float> EvaluateGlobalStats();
}

[DisallowMultipleComponent]
public sealed class GlobalStatSource : MonoBehaviour, IGlobalStatSource
{
    [SerializeField] private List<GlobalStatEntry> baseStats = new List<GlobalStatEntry>();

    public IReadOnlyList<GlobalStatEntry> BaseStats => baseStats;

    public Dictionary<GlobalStatKey, float> EvaluateGlobalStats()
    {
        var result = new Dictionary<GlobalStatKey, float>();
        if (baseStats == null)
            return result;

        foreach (var entry in baseStats)
        {
            result[entry.Key] = entry.Value;
        }

        return result;
    }

    public float GetGlobalStat(GlobalStatKey key)
    {
        var stats = EvaluateGlobalStats();
        return stats != null && stats.TryGetValue(key, out var value) ? value : 0f;
    }
}
