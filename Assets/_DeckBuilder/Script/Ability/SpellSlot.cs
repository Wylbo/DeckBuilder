using System;
using UnityEngine;

[Serializable]
public class SpellSlot
{
	[SerializeField, InlineEditor]
	public Ability Ability;

	[ReadOnly]
	public Timer cooldown;


	public bool CanCast => !cooldown.IsRunning;

	private AbilityCaster caster = null;

	public void Cast(AbilityCaster caster, Vector3 worldPos)
	{
		this.caster = caster;

		Ability.Cast(worldPos);
		cooldown = new Timer(Ability.Cooldown);
		cooldown.Start();
	}

	public void UpdateCooldown(float dt)
	{
		cooldown.Update(dt);
	}

}
