using System.Collections.Generic;
using UnityEngine;

public sealed class AbilityCastContext
{
	public AbilityBehaviourContext BehaviourContext { get; }

	public Ability Ability => BehaviourContext?.Ability;
	public AbilityCaster Caster => BehaviourContext?.Caster;
	public Movement Movement => BehaviourContext?.Movement;
	public ProjectileLauncher ProjectileLauncher => BehaviourContext?.ProjectileLauncher;

	public Vector3 TargetPoint { get; private set; }
	public Vector3 AimPoint { get; private set; }
	public bool IsHeld { get; private set; }
	public Targetable Target { get; set; }
	public float ChannelRatio { get; set; }
	public Component[] LastLaunchedProjectiles { get; set; }

	private readonly Dictionary<string, object> sharedValues = new Dictionary<string, object>();
	private readonly Dictionary<AbilityStatKey, float> sharedStatOverrides = new Dictionary<AbilityStatKey, float>();

	public AbilityCastContext(AbilityBehaviourContext behaviourContext, Vector3 targetPoint, bool isHeld)
	{
		BehaviourContext = behaviourContext;
		TargetPoint = targetPoint;
		AimPoint = targetPoint;
		IsHeld = isHeld;
		ChannelRatio = 0f;
	}

	public void SetTargetPoint(Vector3 targetPoint)
	{
		TargetPoint = targetPoint;
	}

	public void SetAimPoint(Vector3 aimPoint)
	{
		AimPoint = aimPoint;
	}

	public void UpdateHoldState(bool isHeld)
	{
		IsHeld = isHeld;
	}

	public T[] GetLastLaunchedProjectiles<T>() where T : Component
	{
		if (LastLaunchedProjectiles == null)
			return null;

		T[] buffer = new T[LastLaunchedProjectiles.Length];
		for (int i = 0; i < LastLaunchedProjectiles.Length; i++)
			buffer[i] = LastLaunchedProjectiles[i] as T;

		return buffer;
	}

	public void SetSharedValue<T>(string key, T value)
	{
		if (string.IsNullOrEmpty(key))
			return;

		sharedValues[key] = value;
	}

	public bool TryGetSharedValue<T>(string key, out T value)
	{
		if (!string.IsNullOrEmpty(key) && sharedValues.TryGetValue(key, out var boxed) && boxed is T cast)
		{
			value = cast;
			return true;
		}

		value = default;
		return false;
	}

	public void SetSharedStatOverride(AbilityStatKey key, float value)
	{
		sharedStatOverrides[key] = value;
	}

	public bool TryGetSharedStatOverride(AbilityStatKey key, out float value)
	{
		return sharedStatOverrides.TryGetValue(key, out value);
	}
}
