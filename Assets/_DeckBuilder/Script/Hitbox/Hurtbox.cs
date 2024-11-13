using UnityEngine;
using UnityEngine.Events;

public class Hurtbox : MonoBehaviour, IDamageable
{
    [SerializeField]
    private Collider hurtboxCollider;
    private Character owner;
    public Character Owner => owner;

    public event UnityAction<DamageInstance> On_DamageReceived;

    public void SetOwner(Character character)
    {
        owner = character;
    }

    public void TakeDamage(DamageInstance damageInstance)
    {
        On_DamageReceived?.Invoke(damageInstance);
    }
}