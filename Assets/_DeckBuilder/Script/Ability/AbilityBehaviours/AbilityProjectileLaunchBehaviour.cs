using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AbilityProjectileLaunchBehaviour : AbilityBehaviour, IRequireAbilityStats
{
	public enum LaunchPosition
	{
		Caster,
		TargetPoint
	}

	[SerializeField] private Projectile projectile;
	[SerializeField] private LaunchPosition launchPosition = LaunchPosition.Caster;
	[SerializeField] private bool alignWithCasterRotation = true;
	[SerializeField] private bool applyScaleStat = false;
	[SerializeField] private AbilityStatKey scaleStatKey = AbilityStatKey.AOEScale;
	[SerializeField] private bool applyProjectileCount = true;
	[SerializeField] private AbilityStatKey projectileCountStat = AbilityStatKey.ProjectileCount;
	[SerializeField] private bool applySpreadStat = true;
	[SerializeField] private AbilityStatKey spreadStatKey = AbilityStatKey.ProjectileSpreadAngle;
	[SerializeField] private bool applyMaxSpreadStat = true;
	[SerializeField] private AbilityStatKey maxSpreadStatKey = AbilityStatKey.ProjectileMaxSpreadAngle;
	[SerializeField] private bool applyVerticalOffsetStat = false;
	[SerializeField] private AbilityStatKey verticalOffsetStatKey = AbilityStatKey.ProjectileVerticalOffset;
	[SerializeField] private ProjectileLauncher.MultipleProjectileFireMode fireMode = ProjectileLauncher.MultipleProjectileFireMode.Fan;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (projectile == null || context?.ProjectileLauncher == null)
			return;

		var launcher = context.ProjectileLauncher.SetProjectile(projectile);

		float yOffset = applyVerticalOffsetStat ? context.Ability.GetEvaluatedStatValue(verticalOffsetStatKey) : 0f;
		switch (launchPosition)
		{
			case LaunchPosition.Caster:
				{
					Vector3 pos = context.Caster.transform.position;
					pos.y += yOffset;
					launcher.SetPosition(pos);
					if (alignWithCasterRotation)
						launcher.SetRotation(context.Caster.transform.rotation);
					break;
				}
			case LaunchPosition.TargetPoint:
				{
					Vector3 pos = context.TargetPoint;
					pos.y += yOffset;
					launcher.SetPosition(pos);
					break;
				}
		}

		launcher.SetMultipleProjectileFireMode(fireMode);

		if (applyScaleStat)
			launcher.SetScale(context.Ability.GetEvaluatedStatValue(scaleStatKey));

		if (applyProjectileCount)
			launcher.SetProjectileCount(Mathf.RoundToInt(context.Ability.GetEvaluatedStatValue(projectileCountStat)));

		if (applySpreadStat)
			launcher.SetSpreadAngle(context.Ability.GetEvaluatedStatValue(spreadStatKey));

		if (applyMaxSpreadStat)
			launcher.SetMaxSpreadAngle(context.Ability.GetEvaluatedStatValue(maxSpreadStatKey));

		var launched = launcher.Launch<Projectile>();
		context.LastLaunchedProjectiles = launched;
	}

	public IEnumerable<AbilityStatKey> GetRequiredStatKeys()
	{
		if (applyScaleStat) yield return scaleStatKey;
		if (applyProjectileCount) yield return projectileCountStat;
		if (applySpreadStat) yield return spreadStatKey;
		if (applyMaxSpreadStat) yield return maxSpreadStatKey;
		if (applyVerticalOffsetStat) yield return verticalOffsetStatKey;
	}
}
