using System;
using UnityEngine;

[Serializable]
public class AbilityDamageTargetBehaviour : AbilityBehaviour
{
	[SerializeField] private int damage;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (context?.Target == null)
			return;

		Character character = context.Target.Character;
		if (character != null)
			character.TakeDamage(damage);
	}
}
