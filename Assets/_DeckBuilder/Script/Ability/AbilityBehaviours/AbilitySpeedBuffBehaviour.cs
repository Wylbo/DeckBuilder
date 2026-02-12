using System;
using UnityEngine;

/// <summary>
/// Applies a global movement speed modifier on cast start and removes it on cast end.
/// The modifier is a GlobalModifier ScriptableObject configured in the Inspector.
/// </summary>
[Serializable]
public class AbilitySpeedBuffBehaviour : AbilityBehaviour
{
	[SerializeField]
	[Tooltip("GlobalModifier asset with MovementSpeed operations to apply during the buff")]
	private GlobalModifier speedModifier;

	public override void OnCastStarted(AbilityCastContext context)
	{
		if (speedModifier == null)
			return;

		StatsModifierManager modifierManager = context.BehaviourContext?.ModifierManager;
		if (modifierManager == null)
			return;

		modifierManager.AddGlobalModifier(speedModifier);
	}

	public override void OnCastEnded(AbilityCastContext context, bool wasSuccessful)
	{
		if (speedModifier == null)
			return;

		StatsModifierManager modifierManager = context.BehaviourContext?.ModifierManager;
		if (modifierManager == null)
			return;

		modifierManager.RemoveGlobalModifier(speedModifier);
	}
}
