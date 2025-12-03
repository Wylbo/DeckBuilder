using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Debuff", menuName = FileName.Debuff + "Debuff", order = 0)]
public class ScriptableDebuff : ScriptableObject
{
    [SerializeField] private float duration = 0f;
    [SerializeField] private DebuffStackingPolicy stackingPolicy = DebuffStackingPolicy.Stack;
    [SerializeField] private DebuffDurationPolicy durationPolicy = DebuffDurationPolicy.Refresh;
    [SerializeField] private List<AbilityModifier> abilityModifiers = new List<AbilityModifier>();
    [SerializeField] private List<GlobalModifier> globalModifiers = new List<GlobalModifier>();

    public DebuffStackingPolicy StackingPolicy => stackingPolicy;
    public DebuffDurationPolicy DurationPolicy => durationPolicy;
    public float Duration => duration;
    public IReadOnlyList<AbilityModifier> AbilityModifiers => abilityModifiers;
    public IReadOnlyList<GlobalModifier> GlobalModifiers => globalModifiers;
}

