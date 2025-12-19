using System.Collections.Generic;
using UnityEngine;

public static class GlobalModifierRuntime
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
    public static Dictionary<GlobalStatKey, float> Evaluate(
        IEnumerable<GlobalStatEntry> baseStats,
        IEnumerable<GlobalModifier> activeModifiers)
    {
        Dictionary<GlobalStatKey, float> result = new Dictionary<GlobalStatKey, float>();

        if (baseStats != null)
        {
            foreach (GlobalStatEntry stat in baseStats)
            {
                result[stat.Key] = stat.Value;
            }
        }

        List<GlobalModifier> modifiers = new List<GlobalModifier>();
        if (activeModifiers != null)
        {
            foreach (GlobalModifier modifier in activeModifiers)
            {
                if (modifier != null)
                {
                    modifiers.Add(modifier);
                }
            }
        }

        if (modifiers.Count == 0)
        {
            return result;
        }

        List<GlobalModifier> expanded = new List<GlobalModifier>();
        Dictionary<string, List<GlobalModifier>> grouped = new Dictionary<string, List<GlobalModifier>>();

        foreach (GlobalModifier modifier in modifiers)
        {
            string stackGroup = modifier.StackGroup ?? string.Empty;
            if (!grouped.TryGetValue(stackGroup, out List<GlobalModifier> groupList))
            {
                groupList = new List<GlobalModifier>();
                grouped[stackGroup] = groupList;
            }

            groupList.Add(modifier);
        }

        foreach (KeyValuePair<string, List<GlobalModifier>> groupEntry in grouped)
        {
            List<GlobalModifier> groupModifiers = groupEntry.Value;
            groupModifiers.Sort((first, second) => first.Order.CompareTo(second.Order));

            if (string.IsNullOrEmpty(groupEntry.Key))
            {
                expanded.AddRange(groupModifiers);
                continue;
            }

            int stackLimit = Mathf.Min(groupModifiers.Count, Mathf.Max(1, groupModifiers[0].MaxStacks));
            for (int i = 0; i < stackLimit; i++)
            {
                expanded.Add(groupModifiers[i]);
            }
        }

        expanded.Sort((first, second) => first.Order.CompareTo(second.Order));

        foreach (GlobalModifier modifier in expanded)
        {
            foreach (GlobalStatOp operation in modifier.Operations)
            {
                Apply(ref result, operation);
            }
        }

        return result;
    }
    #endregion

    #region Private Methods
    private static void Apply(ref Dictionary<GlobalStatKey, float> result, GlobalStatOp op)
    {
        if (!result.TryGetValue(op.Key, out float value))
        {
            value = 0f;
        }

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
    #endregion
}
