using System;
using UnityEngine;

[Serializable]
[RequiresAbilityBehaviour(typeof(AbilityChannelBehaviour), requirePriorOccurrence: true)]
public class AbilityRangeIndicatorBehaviour : AbilityBehaviour
{
	[SerializeField] private RangeIndicator rangeIndicator;
	[SerializeField] private float minDistance = 5f;
	[SerializeField] private float maxDistance = 10f;

	private RangeIndicator spawnedIndicator;

	public override bool RequiresUpdate => true;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (rangeIndicator == null || context?.Caster == null)
			return;

		spawnedIndicator = PoolManager.Provide<RangeIndicator>(
			rangeIndicator.gameObject,
			context.Caster.transform.position + Vector3.down,
			Quaternion.identity,
			context.Caster.transform
		);

		spawnedIndicator.SetScale(minDistance * 2f);
	}

	public override void OnCastUpdated(AbilityCastContext context, float deltaTime)
	{
		if (spawnedIndicator == null)
			return;

		float distance = Mathf.Lerp(minDistance, maxDistance, context.ChannelRatio);
		spawnedIndicator.SetScale(distance * 2f);
	}

	public override void OnCastEnded(AbilityCastContext context, bool wasSuccessful)
	{
		if (spawnedIndicator == null)
			return;

		spawnedIndicator.SetScale(minDistance * 2f);
		PoolManager.Release(spawnedIndicator.gameObject);
		spawnedIndicator = null;
	}
}
