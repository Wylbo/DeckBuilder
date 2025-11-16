using System;
using UnityEngine;

public enum AbilityStatKey
{
    Cooldown,
    Damage,
    AOEScale,

    ProjectileCount,
    ProjectileSpeed,
    ProjectileSpreadAngle,
    ProjectileMaxSpreadAngle,
    ProjectileVerticalOffset,

    // Channeling
    ChannelDuration,

    // Movement
    DashDistance,
    DashSpeed,

    // Projectile arrangements / offsets
    VolleySpacing,
    VolleyVerticalOffset
}

[Serializable]
public struct AbilityStatEntry
{
    public AbilityStatKey Key;
    public float Value;
}

public enum AbilityStatOpType
{
    Add,
    Multiply,
    Override,
    Min,
    Max
}

[Serializable]
public struct AbilityStatOp
{
    public AbilityStatKey Key;
    public AbilityStatOpType OpType;
    public float Value;
}
