using UnityEngine;

[CreateAssetMenu(fileName = nameof(AOEModifier), menuName = FileName.AbilityModifier + nameof(AOEModifier))]
public class AOEModifier : AbilityModifier
{
    [SerializeField] private float AOEScalePercent;
    public override void Apply(Ability ability)
    {
        if (ability is IHasAOE aoe)
            aoe.AddAOEScale(AOEScalePercent);
    }
}
