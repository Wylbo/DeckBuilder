using System;
using UnityEngine;

public class Character : Entity
{
	[SerializeField]
	private Movement movement;
	[SerializeField]
	private AbilityCaster abilityCaster;
	[SerializeField]
	private Health health;
	[SerializeField]
	private Hurtbox hurtbox;

	public Movement Movement => movement;

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

	public void EndHold(int index, Vector3 worldPos)
	{
		abilityCaster.EndHold(index, worldPos);
	}


	private void Health_On_Empty()
	{
		Die();
	}

	private void Hurtbox_On_DamageReceived(DamageInstance damageInstance)
	{
		health.AddOrRemoveHealth(-damageInstance.Damage);
	}

	private void Die()
	{
		Debug.Log($"[{nameof(Character)}] {name} died");
	}
}
