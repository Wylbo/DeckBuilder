using System;
using UnityEngine;

[Serializable]
public class AbilityDashBehaviour : AbilityBehaviour
{
	[SerializeField] private Movement.DashData dashData;
	[SerializeField] private AbilityStatKey dashDistanceStat = AbilityStatKey.DashDistance;
	[SerializeField] private AbilityStatKey dashSpeedStat = AbilityStatKey.DashSpeed;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (context?.Movement == null)
			return;

		var data = dashData;
		float dist = context.GetStat(dashDistanceStat);
		float spd = context.GetStat(dashSpeedStat);
		if (dist > 0f) data.dashDistance = dist;
		if (spd > 0f) data.dashSpeed = spd;
		context.Movement.Dash(data, context.TargetPoint);
	}

}
