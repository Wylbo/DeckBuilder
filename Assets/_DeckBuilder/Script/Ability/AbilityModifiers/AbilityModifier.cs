using System.IO;
using UnityEngine;

public abstract class AbilityModifier : ScriptableObject
{
    [SerializeField] private AbilityTagSO targetTag;
    public AbilityTagSO TargetTag => targetTag;

    public bool AppliesTo(Ability ability)
    {
        return ability.HasTag(targetTag);
    }

    public abstract void Apply(Ability ability);
}
