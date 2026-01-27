using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public sealed class AbilityCastContext
{
	public AbilityBehaviourContext BehaviourContext { get; }

	public Ability Ability => BehaviourContext?.Ability;
	public AbilityCaster Caster => BehaviourContext?.Caster;
	public IAbilityMovement Movement => BehaviourContext?.Movement;
	public ProjectileLauncher ProjectileLauncher => BehaviourContext?.ProjectileLauncher;
	public IAbilityExecutor Executor => BehaviourContext?.Executor;
	public IAbilityDebuffService DebuffService => BehaviourContext?.DebuffService;
	public IAbilityStatProvider StatProvider => BehaviourContext?.StatProvider;

	public Vector3 TargetPoint { get; private set; }
	public Vector3 AimPoint { get; private set; }
	public bool IsHeld { get; private set; }
	public float ChannelRatio { get; set; }

	private readonly Dictionary<string, object> sharedValues = new Dictionary<string, object>();
	private readonly Dictionary<AbilityStatKey, float> sharedStatOverrides = new Dictionary<AbilityStatKey, float>();
	private ulong targetNetworkObjectId;
	private ulong[] lastLaunchedProjectileNetworkIds;

	public AbilityCastContext(AbilityBehaviourContext behaviourContext, Vector3 targetPoint, Vector3 aimPoint, bool isHeld)
	{
		BehaviourContext = behaviourContext;
		TargetPoint = targetPoint;
		AimPoint = aimPoint;
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
		if (TryGetLastLaunched(out T[] resolved))
		{
			return resolved;
		}

		return null;
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

	public float GetStat(AbilityStatKey key)
	{
		return Executor != null ? Executor.GetStat(key) : 0f;
	}

	public void LookAt(Vector3 worldPos)
	{
		Executor?.LookAtCastDirection(worldPos);
	}

	public void EndCast(Vector3 worldPos, bool isSuccessful = true)
	{
		Executor?.EndCast(worldPos, isSuccessful);
	}

	public void SetTarget(Targetable target)
	{
		if (target == null)
		{
			targetNetworkObjectId = 0;
			return;
		}

		NetworkObject netObj = target.GetComponentInParent<NetworkObject>();
		targetNetworkObjectId = netObj != null ? netObj.NetworkObjectId : 0;
	}

	public bool TryGetTarget(out Targetable target)
	{
		target = null;
		if (targetNetworkObjectId == 0)
		{
			return false;
		}

		NetworkManager manager = NetworkManager.Singleton;
		if (manager != null &&
		    manager.SpawnManager != null &&
		    manager.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out NetworkObject netObj) &&
		    netObj != null)
		{
			target = netObj.GetComponentInChildren<Targetable>();
		}

		return target != null;
	}

	public void SetLastLaunchedProjectiles(Component[] components)
	{
		if (components == null || components.Length == 0)
		{
			lastLaunchedProjectileNetworkIds = null;
			return;
		}

		List<ulong> ids = new List<ulong>(components.Length);
		foreach (Component component in components)
		{
			if (component == null)
			{
				continue;
			}

			NetworkObject netObj = component.GetComponentInParent<NetworkObject>();
			if (netObj != null)
			{
				ids.Add(netObj.NetworkObjectId);
			}
		}

		lastLaunchedProjectileNetworkIds = ids.Count > 0 ? ids.ToArray() : null;
	}

	public bool TryGetLastLaunched<T>(out T[] buffer) where T : Component
	{
		buffer = null;
		if (lastLaunchedProjectileNetworkIds == null || lastLaunchedProjectileNetworkIds.Length == 0)
		{
			return false;
		}

		NetworkManager manager = NetworkManager.Singleton;
		if (manager == null || manager.SpawnManager == null)
		{
			return false;
		}

		List<T> resolved = new List<T>(lastLaunchedProjectileNetworkIds.Length);
		foreach (ulong id in lastLaunchedProjectileNetworkIds)
		{
			if (manager.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject netObj) && netObj != null)
			{
				T component = netObj.GetComponentInChildren<T>();
				if (component != null)
				{
					resolved.Add(component);
				}
			}
		}

		if (resolved.Count == 0)
		{
			return false;
		}

		buffer = resolved.ToArray();
		return true;
	}
}
