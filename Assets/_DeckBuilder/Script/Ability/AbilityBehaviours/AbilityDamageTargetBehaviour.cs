using System;
using UnityEngine;

[Serializable]
public class AbilityDamageTargetBehaviour : AbilityBehaviour
{
	[SerializeField] private AbilityStatKey damageStat = AbilityStatKey.Damage;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (context?.Target == null)
			return;

		Character character = context.Target.Character;
		if (character != null)
		{
			int dmg = Mathf.RoundToInt(context.Ability.GetEvaluatedStatValue(damageStat));
			character.TakeDamage(dmg);
		}
	}

}
