using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityProjectile), menuName = FileName.Ability + nameof(AbilityProjectile))]
public class AbilityProjectile : Ability
{
	[SerializeField]
	private Projectile projectile;

	protected override void DoCast(Vector3 worldPos)
	{
		Caster.ProjectileLauncher.LaunchProjectile(projectile);

		base.DoCast(worldPos);
	}
}
