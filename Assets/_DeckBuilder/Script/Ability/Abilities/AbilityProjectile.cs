using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityProjectile), menuName = FileName.Abilities + nameof(AbilityProjectile))]
public class AbilityProjectile : Ability
{
	[SerializeField]
	private Projectile projectile;

	protected override void DoCast(Vector3 worldPos)
	{
		Caster.ProjectileLauncher.SetProjectile(projectile)
		 	.AtCasterPosition()
			.SetRotation(Caster.transform.rotation)
			.SetProjectileCount((int)GetEvaluatedStatValue(AbilityStatKey.ProjectileCount))
			.SetMultipleProjectileFireMode(ProjectileLauncher.MultipleProjectileFireMode.Fan)
			.SetSpreadAngle(GetEvaluatedStatValue(AbilityStatKey.ProjectileSpreadAngle))
			.SetMaxSpreadAngle(GetEvaluatedStatValue(AbilityStatKey.ProjectileMaxSpreadAngle))
			.Launch<Projectile>();

		base.DoCast(worldPos);
	}
}
