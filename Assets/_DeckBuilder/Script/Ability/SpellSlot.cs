using System;
using UnityEngine;

[Serializable]
public class SpellSlot
{
	[SerializeField]
	public Ability Ability;

	[HideInInspector]
	public Timer cooldown;

	public bool CanCast => !cooldown.IsRunning;

	private AbilityCaster caster = null;

	public void Cast(AbilityCaster caster, Vector3 worldPos)
	{
		this.caster = caster;
		if (Ability.RotateCasterToCastDirection)
			LookAtCastDirection(worldPos);

		Ability.Cast();
		cooldown = new Timer(Ability.Cooldown);
		cooldown.Start();
	}

	public void UpdateCooldown(float dt)
	{
		cooldown.Update(dt);
	}


	private void LookAtCastDirection(Vector3 worldPos)
	{
		Vector3 castDirection = worldPos - caster.transform.position;
		castDirection.y = 0;
		Debug.DrawRay(caster.transform.position, castDirection, Color.yellow, 1f);
		caster.transform.LookAt(caster.transform.position + castDirection);
	}
}
