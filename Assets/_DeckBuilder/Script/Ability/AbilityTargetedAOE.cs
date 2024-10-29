using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityTargetedAOE), menuName = FileName.Ability + nameof(AbilityTargetedAOE))]
public class AbilityTargetedAOE : Ability
{
	[SerializeField]
	private Projectile projectile;
	protected override void DoCast(Vector3 worldPos)
	{
		Caster.ProjectileLauncher.LaunchProjectile(projectile, worldPos);
		base.DoCast(worldPos);
	}
}
