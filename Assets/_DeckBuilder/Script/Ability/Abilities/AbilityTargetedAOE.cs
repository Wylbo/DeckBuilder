using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityTargetedAOE), menuName = FileName.Abilities + nameof(AbilityTargetedAOE))]
public class AbilityTargetedAOE : Ability//, IHasAOE
{
	[SerializeField]
	private Projectile projectile;

	protected float baseScale = 1;

	protected override IEnumerable<AbilityStatEntry> GetBaseStats()
	{
		foreach (var stat in base.GetBaseStats())
			yield return stat;

		yield return new AbilityStatEntry { Key = AbilityStatKey.AOEScale, Value = baseScale };

	}

	protected override void DoCast(Vector3 worldPos)
	{
		var stats = EvaluateStats(Caster.ModifierManager.ActiveModifiers);
		Caster.ProjectileLauncher.LaunchProjectile(projectile, worldPos, StatOr(stats, AbilityStatKey.AOEScale, baseScale));
		base.DoCast(worldPos);
	}

}
