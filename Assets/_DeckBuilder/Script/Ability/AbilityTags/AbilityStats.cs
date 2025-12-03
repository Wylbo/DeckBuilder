using System;
using UnityEngine;

// Ability stats are the values an ability consumes at runtime (cooldown, damage, etc.).
// They can be produced from flat values or derived from caster-wide global stats.
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

// Stats defined on the caster (shared across abilities).
public enum GlobalStatKey
{
    AttackPower,
    AOEScale,
    CooldownReduction,
    MovementSpeed,

}

// Describes how an ability stat should be sourced.
public enum AbilityStatSource
{
    Flat = 0,        // Use the Value as-is.
    RatioToGlobal = 1, // Multiply Value by the selected GlobalKey.
    CopyGlobal = 2     // Copy the selected GlobalKey directly.
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

[Serializable]
public struct GlobalStatOp
{
    public GlobalStatKey Key;
    public AbilityStatOpType OpType;
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
