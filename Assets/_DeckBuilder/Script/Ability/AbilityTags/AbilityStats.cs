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

// Stats defined on the caster (shared across abilities)
public enum GlobalStatKey
{
    AttackPower,
    AOEScale,
    CooldownReduction
}

public enum AbilityStatSource
{
    Flat = 0,
    RatioToGlobal = 1,
    CopyGlobal = 2
}

[Serializable]
public struct AbilityStatEntry
{
    public AbilityStatKey Key;
    public AbilityStatSource Source;

    [Tooltip("When Source=Flat: absolute value. When Source=RatioToGlobal: multiplier applied to the selected GlobalKey. Ignored for CopyGlobal.")]
    public float Value;

    [Tooltip("Global stat used when Source is RatioToGlobal or CopyGlobal.")]
    public GlobalStatKey GlobalKey;
}

[Serializable]
public struct GlobalStatEntry
{
    public GlobalStatKey Key;
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
