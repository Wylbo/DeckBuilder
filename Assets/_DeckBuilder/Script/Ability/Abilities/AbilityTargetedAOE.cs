using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityTargetedAOE), menuName = FileName.Abilities + nameof(AbilityTargetedAOE))]
public class AbilityTargetedAOE : Ability//, IHasAOE
{
	[SerializeField]
	private Projectile projectile;

	// protected float baseScale = 1;



	protected override void DoCast(Vector3 worldPos)
	{
		var stats = EvaluateStats(Caster.ModifierManager.ActiveModifiers);
		Caster.ProjectileLauncher.SetProjectile(projectile)
			.SetPosition(worldPos)
			.SetScale(GetEvaluatedStatValue(AbilityStatKey.AOEScale))
			.Launch<Projectile>();

		base.DoCast(worldPos);
	}

}
