public interface IBaseAbilityModifier
{
    void ResetModifiers();
}
public interface IHasCooldown : IBaseAbilityModifier
{
    void AddCooldownFlatOffset(float offset);
    void AddCooldownPercentOffet(float percent);
    float GetModifiedCooldown();
}

public interface IHasDamage : IBaseAbilityModifier
{
    void AddDamagePercentBonus(float percent);
    void AddDamageFlatBonus(float flat);
}

public interface IHasAOE : IBaseAbilityModifier
{
    void AddAOEScale(float percent);
    float GetModifiedScale();

}

