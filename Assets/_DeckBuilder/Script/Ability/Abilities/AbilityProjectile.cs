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
			.Launch<Projectile>();

		base.DoCast(worldPos);
	}
}
