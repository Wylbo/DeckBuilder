using static Movement;

public struct DamageInstance
{
    public int Damage;
    public IDamager Damager;
    public DashData knockBackData;

    public DamageInstance(DamageInstance other)
    {
        Damage = other.Damage;
        Damager = other.Damager;
        knockBackData = other.knockBackData;
    }
}