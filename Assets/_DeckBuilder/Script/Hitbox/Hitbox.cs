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
        if (other.TryGetComponent(out IDamageable damageable) && damageable.Owner != Owner)
        {
            Debug.Log($"[{nameof(Hitbox)}] Hit something");
            damageable.TakeDamage(CreateDamageInstance());
        }
    }

    public void SetOwner(Character character)
    {
        owner = character;
    }
}