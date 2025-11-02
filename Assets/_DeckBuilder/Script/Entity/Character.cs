using System;
using UnityEngine;
using UnityEngine.Events;

public class Character : Entity
{
	[SerializeField] CharacterVisual characterVisual;
	[SerializeField] Movement movement;
	[SerializeField] AbilityCaster abilityCaster;
	[SerializeField] Health health;
	[SerializeField] Hurtbox hurtbox;

	public Movement Movement => movement;
	public Health Health => health;
	public bool IsDead => Health.Value > 0;

	public event UnityAction On_Died;

	private void OnEnable()
	{
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
		if (IsDead) return;

		Debug.Log($"[{nameof(Character)}] {name} died");
		On_Died?.Invoke();
		characterVisual.Dissolve();
	}
}
