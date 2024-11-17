using System;
using UnityEngine;

[Serializable]
public class SpellSlot
{
	[SerializeField, InlineEditor]
	public Ability Ability;

	[ReadOnly]
	public Timer cooldown;

	bool isHeld = false;

	public bool CanCast => !cooldown.IsRunning;

	private AbilityCaster caster = null;

	public bool Initialize(AbilityCaster caster)
	{
		if (!Ability)
			return false;
		Ability = UnityEngine.Object.Instantiate(Ability);
		Ability.Initialize(caster);
		return true;
	}
	public void Cast(AbilityCaster caster, Vector3 worldPos)
	{
		this.caster = caster;

		isHeld = true;
		Ability.On_EndCast += Ability_OnEndCast;
		Ability.Cast(worldPos, isHeld);
	}

	public void EndHold(AbilityCaster caster, Vector3 worldPos)
	{
		this.caster = caster;
		isHeld = false;
		Ability.EndHold(worldPos);
	}
	public void UpdateCooldown(float dt)
	{
		cooldown.Update(dt);
	}

	private void Ability_OnEndCast(bool isSucessful)
	{
		Ability.On_EndCast -= Ability_OnEndCast;

		if (!isSucessful)
			return;

		cooldown = new Timer(Ability.Cooldown);
		cooldown.Start();
	}
}
