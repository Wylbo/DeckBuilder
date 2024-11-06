using UnityEngine;
using UnityEngine.Events;

public class Hurtbox : MonoBehaviour, IDamageable
{
    [SerializeField]
    private Collider hurtboxCollider;

    public event UnityAction<DamageInstance> On_DamageReceived;
    public void TakeDamage(DamageInstance damageInstance)
    {
        On_DamageReceived?.Invoke(damageInstance);
    }
}