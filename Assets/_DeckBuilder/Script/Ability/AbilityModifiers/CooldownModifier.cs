using UnityEngine;

[CreateAssetMenu(fileName = nameof(CooldownModifier), menuName = FileName.AbilityModifier + nameof(CooldownModifier))]
public class CooldownModifier : AbilityModifier
{
    [SerializeField] private float flatOffset;

    [SerializeField, Range(0.0f, 1.0f)] private float percentReduction;
    public override void Apply(Ability ability)
    {
        if (ability is IHasCooldown cd)
        {
            cd.AddCooldownFlatOffset(flatOffset);
            cd.AddCooldownPercentOffet(percentReduction);
        }

    }
}
