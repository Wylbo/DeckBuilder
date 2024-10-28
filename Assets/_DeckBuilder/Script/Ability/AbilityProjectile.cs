using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityProjectile), menuName = FileName.Ability + nameof(AbilityProjectile))]
public class AbilityProjectile : Ability
{
	[SerializeField]
	private Projectile projectile;

	public override void Cast()
	{
		Caster.ProjectileLauncher.LaunchProjectile(projectile);
	}
}
