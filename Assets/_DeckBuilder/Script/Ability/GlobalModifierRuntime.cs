using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GlobalModifierRuntime
{
    public static Dictionary<GlobalStatKey, float> Evaluate(IEnumerable<GlobalStatEntry> baseStats, IEnumerable<GlobalModifier> activeModifiers)
    {
        var result = new Dictionary<GlobalStatKey, float>();

        if (baseStats != null)
        {
            foreach (var s in baseStats)
                result[s.Key] = s.Value;
        }

        var modifiers = activeModifiers?.Where(m => m != null).ToList() ?? new List<GlobalModifier>();
        if (modifiers.Count == 0)
            return result;

        // Handle stacking and ordering
        var expanded = new List<GlobalModifier>();
        var groups = modifiers.GroupBy(m => m.StackGroup);

        foreach (var g in groups)
        {
            if (string.IsNullOrEmpty(g.Key))
            {
                expanded.AddRange(g);
            }
            else
            {
                int count = Mathf.Min(g.Count(), Mathf.Max(1, g.First().MaxStacks));
                expanded.AddRange(g.Take(count));
            }
        }

        expanded.Sort((a, b) => a.Order.CompareTo(b.Order));

        foreach (var mod in expanded)
        {
            foreach (var op in mod.Operations)
            {
                Apply(ref result, op);
            }
        }

        return result;
    }

    private static void Apply(ref Dictionary<GlobalStatKey, float> result, GlobalStatOp op)
    {
        if (!result.TryGetValue(op.Key, out float value))
            value = 0f;

        switch (op.OpType)
        {
            case AbilityStatOpType.Add:
                value += op.Value;
                break;
            case AbilityStatOpType.Multiply:
                value *= op.Value;
                break;
            case AbilityStatOpType.Override:
                value = op.Value;
                break;
            case AbilityStatOpType.Min:
                value = Mathf.Min(value, op.Value);
                break;
            case AbilityStatOpType.Max:
                value = Mathf.Max(value, op.Value);
                break;
        }

        result[op.Key] = value;
    }
}
