using System;
using UnityEngine;

[Serializable]
public class AbilityDashBehaviour : AbilityBehaviour
{
	[SerializeField] private Movement.DashData dashData;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (context?.Movement == null)
			return;

		context.Movement.Dash(dashData, context.TargetPoint);
	}
}
