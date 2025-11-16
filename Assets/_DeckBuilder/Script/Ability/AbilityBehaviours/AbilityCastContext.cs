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
}
