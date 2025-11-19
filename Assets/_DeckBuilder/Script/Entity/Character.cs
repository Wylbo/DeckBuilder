using UnityEngine;
using UnityEngine.Events;

public class Character : Entity
{
    [SerializeField] private CharacterVisual characterVisual;
    [SerializeField] private Movement movement;
    [SerializeField] private AbilityCaster abilityCaster;
    [SerializeField] private Health health;
    [SerializeField] private Hurtbox hurtbox;

    private bool isDead;

    public Movement Movement => movement;
    public Health Health => health;
    public bool IsDead => isDead;

    public event UnityAction On_Died;
    public event UnityAction<Character> On_ReadyForRelease;

    private void OnEnable()
    {
        isDead = false;
        health.On_Empty += Health_On_Empty;
        hurtbox.On_DamageReceived += Hurtbox_On_DamageReceived;
        hurtbox.SetOwner(this);
    }

    private void OnDisable()
    {
        health.On_Empty -= Health_On_Empty;
        hurtbox.On_DamageReceived -= Hurtbox_On_DamageReceived;
    }

    public bool MoveTo(Vector3 worldTo)
    {
        return movement.MoveTo(worldTo);
    }

    public void CastAbility(int index, Vector3 worldPos)
    {
        abilityCaster.Cast(index, worldPos);
    }

    public void PerformDodge(Vector3 worldPos)
    {
        abilityCaster.CastDodge(worldPos);
    }

    public void EndHold(int index, Vector3 worldPos)
    {
        abilityCaster.EndHold(index, worldPos);
    }

    public void TakeDamage(int damage)
    {
        health.AddOrRemoveHealth(-damage);
    }

    private void Hurtbox_On_DamageReceived(DamageInstance damageInstance)
    {
        TakeDamage(damageInstance.Damage);
    }

    private void Health_On_Empty()
    {
        Die();
    }

    protected virtual void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log($"[{nameof(Character)}] {name} died");
        On_Died?.Invoke();

        if (characterVisual != null)
        {
            characterVisual.Dissolve(NotifyReadyForRelease);
        }
        else
        {
            NotifyReadyForRelease();
        }
    }

    private void NotifyReadyForRelease()
    {
        if (On_ReadyForRelease != null)
        {
            On_ReadyForRelease.Invoke(this);
            return;
        }

        gameObject.SetActive(false);
    }
}
