using System;
using UnityEngine;

public enum AbilityStatKey
{
    Cooldown,
    Damage,
    AOEScale,
    ProjectileSpeed
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