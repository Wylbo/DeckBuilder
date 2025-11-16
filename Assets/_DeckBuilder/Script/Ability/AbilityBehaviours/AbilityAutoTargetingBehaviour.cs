using System;
using UnityEngine;

[Serializable]
public class AbilityAutoTargetingBehaviour : AbilityBehaviour
{
	[SerializeField] private float autoTargetingRange = 0.5f;
	[SerializeField] private LayerMask targetableLayerMask;

	private static readonly Collider[] overlapBuffer = new Collider[16];

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (context == null)
			return;

		int hits = Physics.OverlapSphereNonAlloc(
			context.TargetPoint,
			autoTargetingRange,
			overlapBuffer,
			targetableLayerMask
		);

		Targetable closest = null;
		float closestDist = float.MaxValue;

		for (int i = 0; i < hits; i++)
		{
			Collider col = overlapBuffer[i];
			if (col.TryGetComponent(out Targetable target))
			{
				float sqrDist = (target.transform.position - context.TargetPoint).sqrMagnitude;
				if (sqrDist < closestDist)
				{
					closest = target;
					closestDist = sqrDist;
				}
			}
		}

		context.Target = closest;
		if (closest != null)
		{
			context.SetAimPoint(closest.transform.position);
			context.SetTargetPoint(closest.transform.position);
		}
	}
}
