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

		context.Ability.LookAtCastDirection(context.AimPoint);
		var launchedProjectiles = context.ProjectileLauncher.SetProjectile(projectile)
			.AtCasterPosition()
			.SetRotation(context.Caster.transform.rotation)
			.SetProjectileCount(Mathf.RoundToInt(context.Ability.GetEvaluatedStatValue(AbilityStatKey.ProjectileCount)))
			.SetMultipleProjectileFireMode(ProjectileLauncher.MultipleProjectileFireMode.Fan)
			.SetSpreadAngle(context.Ability.GetEvaluatedStatValue(AbilityStatKey.ProjectileSpreadAngle))
			.SetMaxSpreadAngle(context.Ability.GetEvaluatedStatValue(AbilityStatKey.ProjectileMaxSpreadAngle))
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
