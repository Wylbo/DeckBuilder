using System;
using UnityEngine;

public class Hitbox : MonoBehaviour, IDamager, IOwnable
{
    [SerializeField]
    private DamageInstance damageInstance;

    protected Character owner;
    public Character Owner => owner;

    public DamageInstance CreateDamageInstance()
    {
        return new DamageInstance(damageInstance);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Owner == null)
        {
            return;
        }

        if (other.TryGetComponent(out IDamageable damageable) && Owner.IsHostileTo(damageable.Owner))
        {
            damageable.TakeDamage(CreateDamageInstance());
        }
    }

    public void SetOwner(Character character)
    {
        owner = character;
    }

    public void SetDamage(int damage)
    {
        damageInstance.Damage = damage;
    }
}
