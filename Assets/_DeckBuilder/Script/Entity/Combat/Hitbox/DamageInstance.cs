using System;
using UnityEngine;
using static Movement;

[Serializable]
public struct DamageInstance
{
    [SerializeField]
    public int Damage;
    [SerializeField]
    public IDamager Damager;
    [SerializeField]
    public DashData knockBackData;

    public DamageInstance(DamageInstance other)
    {
        Damage = other.Damage;
        Damager = other.Damager;
        knockBackData = other.knockBackData;
    }
}