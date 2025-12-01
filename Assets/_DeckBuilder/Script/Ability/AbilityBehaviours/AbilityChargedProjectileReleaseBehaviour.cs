using System;
using UnityEngine;

[Serializable]
[RequiresAbilityBehaviour(typeof(AbilityChannelBehaviour))]
public class AbilityChargedProjectileReleaseBehaviour : AbilityBehaviour
{
	[SerializeField] private LinearProjectile projectile;
	[SerializeField] private float minDistance = 5f;
	[SerializeField] private float maxDistance = 10f;
	[SerializeField] private int minDamage = 10;
	[SerializeField] private int maxDamage = 20;

	public override void OnCastEnded(AbilityCastContext context, bool wasSuccessful)
	{
		if (projectile == null || context?.ProjectileLauncher == null)
			return;

		context.LookAt(context.AimPoint);
		var launchedProjectiles = context.ProjectileLauncher.SetProjectile(projectile)
			.AtCasterPosition()
			.SetRotation(context.Caster.transform.rotation)
			.SetProjectileCount(Mathf.RoundToInt(context.GetStat(AbilityStatKey.ProjectileCount)))
			.SetMultipleProjectileFireMode(ProjectileLauncher.MultipleProjectileFireMode.Fan)
			.SetSpreadAngle(context.GetStat(AbilityStatKey.ProjectileSpreadAngle))
			.SetMaxSpreadAngle(context.GetStat(AbilityStatKey.ProjectileMaxSpreadAngle))
			.Launch<LinearProjectile>();

		context.LastLaunchedProjectiles = launchedProjectiles;

		float distanceToTravel = Mathf.Lerp(minDistance, maxDistance, context.ChannelRatio);
		foreach (var launchedProjectile in launchedProjectiles)
		{
			launchedProjectile.SetLifeTime(distanceToTravel / launchedProjectile.MaxSpeed);

			Hitbox hitbox = launchedProjectile.GetComponent<Hitbox>();
			if (hitbox != null)
			{
				int damage = Mathf.FloorToInt(Mathf.Lerp(minDamage, maxDamage, context.ChannelRatio));
				hitbox.SetDamage(damage);
			}
		}

	}

}
