public interface IHasCooldown
{
    void AddCooldownFlatOffset(float offset);
    void AddCooldownPercentOffet(float percent);
    void ResetCooldownModifiers();
    float GetModifiedCooldown();
}

public interface IHasDamage
{
    void AddDamagePercentBonus(float percent);
    void AddDamageFlatBonus(float flat);
}

