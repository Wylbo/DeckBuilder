using System;
using UnityEngine;

[Serializable]
public class AbilityDamageTargetBehaviour : AbilityBehaviour
{
	[SerializeField] private AbilityStatKey damageStat = AbilityStatKey.Damage;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (context == null)
			return;

		if (!context.TryGetTarget(out Targetable target))
			return;

		Character character = target.Character;
		if (character != null)
		{
			int dmg = Mathf.RoundToInt(context.GetStat(damageStat));
			character.TakeDamage(dmg);
		}
	}

}
