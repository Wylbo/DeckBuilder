using System;
using UnityEngine;

[Serializable]
[RequiresAbilityBehaviour(typeof(AbilityChannelBehaviour))]
public class AbilityPerpendicularProjectileVolleyBehaviour : AbilityBehaviour
{
	[SerializeField] private Projectile projectile;
	[SerializeField] private AbilityStatKey projectileCountStat = AbilityStatKey.ProjectileCount;
	[SerializeField] private AbilityStatKey spacingStat = AbilityStatKey.VolleySpacing;
	[SerializeField] private AbilityStatKey verticalOffsetStat = AbilityStatKey.VolleyVerticalOffset;

	public override void OnCastEnded(AbilityCastContext context, bool wasSuccessful)
	{
		if (!wasSuccessful || projectile == null || context?.ProjectileLauncher == null || context.Caster == null)
			return;

		float countValue = context.GetStat(projectileCountStat);
		if (context.TryGetSharedStatOverride(projectileCountStat, out float overrideCount))
			countValue = overrideCount;

		int count = Mathf.Max(1, Mathf.RoundToInt(countValue));

		var casterTransform = context.Caster.transform;
		Vector3 forward = casterTransform.forward.normalized;
		if (forward == Vector3.zero)
			forward = Vector3.forward;

		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		if (right == Vector3.zero)
			right = casterTransform.right.normalized;

		float spacing = context.GetStat(spacingStat);
		float vOffset = context.GetStat(verticalOffsetStat);
		float offsetBase = spacing * (count - 1) * 0.5f;

		for (int i = 0; i < count; i++)
		{
			float offset = (spacing * i) - offsetBase;
			Vector3 spawnPos = casterTransform.position + right * offset;
			spawnPos.y += vOffset;

			context.ProjectileLauncher
				.SetProjectile(projectile)
				.SetPosition(spawnPos)
				.SetRotation(Quaternion.LookRotation(forward))
				.SetProjectileCount(1)
				.Launch<Projectile>();
		}
	}

}
