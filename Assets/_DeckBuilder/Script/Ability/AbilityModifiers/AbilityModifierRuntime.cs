using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Internal.Commands;
using UnityEngine;

public static class AbilityModifierRuntime
{
    public static Dictionary<AbilityStatKey, float> Evaluate(GTagSet set /*IReadOnlyList<AbilityTagSO> tags*/, IEnumerable<AbilityStatEntry> baseStats, IEnumerable<AbilityModifier> activeModifiers)
    {
        var result = new Dictionary<AbilityStatKey, float>();

        foreach (var s in baseStats)
            result[s.Key] = s.Value;

        var abilityTags = set;//tags ?? new List<AbilityTagSO>();


        var matching = activeModifiers.Where(mod => mod && mod.Query.Matches(abilityTags.AsTags())).ToList();

        // Handle stacking
        var expanded = new List<AbilityModifier>();
        var groups = matching.GroupBy(m => m.StackGroup);

        foreach (var g in groups)
        {
            if (string.IsNullOrEmpty(g.Key))
            {
                // No stacking group
                expanded.AddRange(g);
            }
            else
            {
                // else stack 
                int count = Mathf.Min(g.Count(), Mathf.Max(1, g.First().MaxStacks));
                expanded.AddRange(g.Take(count));
            }

            // Sort by order
            expanded.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        // Apply operations
        foreach (var mod in expanded)
        {
            foreach (var op in mod.Operations)
            {
                Apply(ref result, op);
            }
        }

        return result;
    }

    private static void Apply(ref Dictionary<AbilityStatKey, float> result, AbilityStatOp op)
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