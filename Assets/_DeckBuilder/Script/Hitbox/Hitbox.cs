using UnityEngine;

public class Hitbox : MonoBehaviour, IDamager
{
    [SerializeField]
    private DamageInstance damageInstance;

    public DamageInstance CreateDamageInstance()
    {
        return new DamageInstance(damageInstance);
    }

    private void OnTriggerEnter(Collider other)
    {
        IDamageable damageable = other.GetComponent<IDamageable>();
        damageable?.TakeDamage(CreateDamageInstance());
    }
}