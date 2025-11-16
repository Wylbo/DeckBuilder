using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[RequiresAbilityBehaviour(typeof(AbilityChannelBehaviour))]
public class AbilityChannelProjectileCountBehaviour : AbilityBehaviour, IRequireAbilityStats
{
	[SerializeField] private AbilityStatKey statKey = AbilityStatKey.ProjectileCount;
	[SerializeField] private int minAdditionalProjectiles = 0;
	[SerializeField] private int maxAdditionalProjectiles = 4;
	[SerializeField] private AnimationCurve channelToCountCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public override void OnCastEnded(AbilityCastContext context, bool wasSuccessful)
	{
		if (!wasSuccessful || context == null)
			return;

		float ratio = Mathf.Clamp01(context.ChannelRatio);
		float curveValue = Mathf.Clamp01(channelToCountCurve.Evaluate(ratio));

		int min = Mathf.Min(minAdditionalProjectiles, maxAdditionalProjectiles);
		int max = Mathf.Max(minAdditionalProjectiles, maxAdditionalProjectiles);
		float additional = Mathf.Lerp(min, max, curveValue);

		float baseCount = context.Ability.GetEvaluatedStatValue(statKey);
		float finalCount = Mathf.Max(1f, baseCount + additional);

		context.SetSharedStatOverride(statKey, finalCount);
	}

	public IEnumerable<AbilityStatKey> GetRequiredStatKeys()
	{
		yield return statKey;
	}
}
