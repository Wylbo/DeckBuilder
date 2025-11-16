using System;
using UnityEngine;

[Serializable]
public class AbilityChannelBehaviour : AbilityBehaviour
{
	[SerializeField] private bool followCursorDuringChanneling = false;
	[SerializeField] private bool holdToChannel = false;
	[SerializeField] private AbilityStatKey channelDurationStat = AbilityStatKey.ChannelDuration;
	[SerializeField, Tooltip("Can a move command be input during channeling")]
	private bool canMoveDuringChanneling = false;
	[SerializeField]
	private bool movingInterruptsChanneling = false;
	[SerializeField]
	private GameObject channelingVFX = null;
	[SerializeField] private LayerMask groundLayerMask = 0;
	[SerializeField, Tooltip("Treat interrupted channels as successful casts")]
	private bool forceSuccessOnInterrupt = false;

	private GameObject spawnedVFX;
	private float elapsed;
	private bool isChanneling;

	public override bool RequiresUpdate => true;
	public override bool BlocksAbilityEnd => true;

	public override void OnCastStarted(AbilityCastContext context)
	{
		elapsed = 0f;
		context.ChannelRatio = 0f;
		isChanneling = true;

		if (!canMoveDuringChanneling)
			context.Movement?.DisableMovement();

		if (channelingVFX != null && context.Caster != null)
		{
			spawnedVFX = PoolManager.Provide(
				channelingVFX,
				context.Caster.transform.position,
				context.Caster.transform.rotation,
				context.Caster.transform,
				PoolManager.PoolType.VFX
			);
		}

		UpdateAimPoint(context);

		float channelDuration = context.Ability.GetEvaluatedStatValue(channelDurationStat);
		if (channelDuration <= 0f)
			CompleteChannel(context, true);
	}

	public override void OnCastUpdated(AbilityCastContext context, float deltaTime)
	{
		if (!isChanneling || context == null)
			return;

		UpdateAimPoint(context);

		if (holdToChannel && !context.IsHeld && elapsed > 0f)
		{
			CompleteChannel(context, false);
			return;
		}

		if (movingInterruptsChanneling && context.Movement != null && context.Movement.IsMoving)
		{
			CompleteChannel(context, false);
			return;
		}

		elapsed += deltaTime;
		float channelDuration = context.Ability.GetEvaluatedStatValue(channelDurationStat);
		if (channelDuration > 0f)
			context.ChannelRatio = Mathf.Clamp01(elapsed / channelDuration);
		else
			context.ChannelRatio = 1f;

		if (elapsed >= channelDuration)
			CompleteChannel(context, true);
	}

	public override void OnCastEnded(AbilityCastContext context, bool wasSuccessful)
	{
		isChanneling = false;
		if (!canMoveDuringChanneling)
			context.Movement?.EnableMovement();

		if (spawnedVFX != null)
		{
			PoolManager.Release(spawnedVFX);
			spawnedVFX = null;
		}
	}

	private void CompleteChannel(AbilityCastContext context, bool naturalSuccess)
	{
		if (!isChanneling)
			return;

		isChanneling = false;
		float channelDuration = context.Ability.GetEvaluatedStatValue(channelDurationStat);
		context.ChannelRatio = Mathf.Clamp01(channelDuration > 0f ? elapsed / channelDuration : 1f);
		bool finalSuccess = naturalSuccess || forceSuccessOnInterrupt;
		context.Ability.EndCast(context.TargetPoint, finalSuccess);
	}

	private void UpdateAimPoint(AbilityCastContext context)
	{
		if (!followCursorDuringChanneling || context?.Caster == null)
			return;

		Vector3 worldPosition = context.Caster.transform.position + context.Caster.transform.forward;
		if (TryGetCursorPosition(out Vector3 cursorPosition))
			worldPosition = cursorPosition;

		context.SetAimPoint(worldPosition);
		context.Ability.LookAtCastDirection(worldPosition);
	}

	private bool TryGetCursorPosition(out Vector3 worldPosition)
	{
		if (Camera.main != null)
		{
			Vector3 viewportPosition = Camera.main.ScreenToViewportPoint(Input.mousePosition);
			Ray ray = Camera.main.ViewportPointToRay(viewportPosition);

			if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f, groundLayerMask))
			{
				worldPosition = hitInfo.point;
				return true;
			}
		}

		worldPosition = Vector3.zero;
		return false;
	}

}
