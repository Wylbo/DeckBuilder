using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class TagSet
{
    [SerializeField] private List<AbilityTagSO> tags = new List<AbilityTagSO>();
    public IReadOnlyList<AbilityTagSO> Tags => tags;

    public bool Contains(AbilityTagSO tag) => tag && tags.Contains(tag);
}


[System.Serializable]
public struct TagQuery
{
    public List<AbilityTagSO> all;
    public List<AbilityTagSO> any;
    public List<AbilityTagSO> none;

    public bool Matches(IReadOnlyList<AbilityTagSO> target)
    {
        if (target == null)
            return false;

        if (all != null)
        {
            foreach (AbilityTagSO tag in all)
            {
                if (!target.Contains(tag))
                    return false;
            }
        }
        if (any != null && any.Count > 0)
        {
            bool hasAny = false;
            foreach (AbilityTagSO tag in any)
            {
                if (target.Contains(tag))
                {
                    hasAny = true;
                    break;
                }
            }
            if (!hasAny)
                return false;
        }

        if (none != null)
        {
            foreach (AbilityTagSO tag in none)
            {
                if (target.Contains(tag))
                    return false;
            }
        }

        return true;
    }
}

