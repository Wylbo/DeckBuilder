using System.Collections.Generic;
using UnityEngine;

public static class AbilityModifierRuntime
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
    public static Dictionary<AbilityStatKey, float> Evaluate(
        GTagSet set,
        IReadOnlyDictionary<AbilityStatKey, float> baseStats,
        IEnumerable<AbilityModifier> activeModifiers)
    {
        Dictionary<AbilityStatKey, float> result = new Dictionary<AbilityStatKey, float>();

        if (baseStats != null)
        {
            foreach (KeyValuePair<AbilityStatKey, float> kvp in baseStats)
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        GTagSet abilityTags = set;

        List<AbilityModifier> matchingModifiers = new List<AbilityModifier>();
        if (activeModifiers != null)
        {
            foreach (AbilityModifier modifier in activeModifiers)
            {
                if (modifier == null)
                {
                    continue;
                }

                if (modifier.Query.Matches(abilityTags.AsTags()))
                {
                    matchingModifiers.Add(modifier);
                }
            }
        }

        List<AbilityModifier> expanded = new List<AbilityModifier>();
        Dictionary<string, List<AbilityModifier>> grouped = new Dictionary<string, List<AbilityModifier>>();

        foreach (AbilityModifier modifier in matchingModifiers)
        {
            string stackGroup = modifier.StackGroup ?? string.Empty;

            if (!grouped.TryGetValue(stackGroup, out List<AbilityModifier> groupList))
            {
                groupList = new List<AbilityModifier>();
                grouped[stackGroup] = groupList;
            }

            groupList.Add(modifier);
        }

        foreach (KeyValuePair<string, List<AbilityModifier>> groupEntry in grouped)
        {
            List<AbilityModifier> groupModifiers = groupEntry.Value;
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

        foreach (AbilityModifier modifier in expanded)
        {
            foreach (AbilityStatOp operation in modifier.Operations)
            {
                Apply(ref result, operation);
            }
        }

        return result;
    }
    #endregion

    #region Private Methods
    private static void Apply(ref Dictionary<AbilityStatKey, float> result, AbilityStatOp op)
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
