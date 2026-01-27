using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Projectile : NetworkBehaviour, IOwnable
{
	[SerializeField]
	protected float lifeTime = 0.0f;
	[SerializeField] private List<TrailRenderer> trailRenderers = new List<TrailRenderer>();

	protected float elapsedLifeTime = 0.0f;

	protected float TimeRatio => lifeTime > 0 ? Mathf.Clamp01(elapsedLifeTime / lifeTime) : 0;

	private Character owner;
	public Character Owner => owner;

	private Hitbox hitbox;
	private Vector3 baseScale;
	private Coroutine trailResetRoutine;
	private NetworkObject networkObject;
	private NetworkTransform networkTransform;

	private void Awake()
	{
		baseScale = transform.localScale;
		networkObject = GetComponent<NetworkObject>();
		networkTransform = GetComponent<NetworkTransform>();
		if (networkTransform == null)
		{
			networkTransform = gameObject.AddComponent<NetworkTransform>();
		}
	}

	protected virtual void OnEnable()
	{
		elapsedLifeTime = 0;
		RestartTrails();
	}

	protected virtual void OnDisable()
	{
		if (trailResetRoutine != null)
		{
			StopCoroutine(trailResetRoutine);
			trailResetRoutine = null;
		}

		SetTrailsEmitting(false);
		ClearTrails();
	}

	protected virtual void Update()
	{
		if (IsSpawned && !IsServer)
			return;

		if (lifeTime <= 0)
			return;

		if (elapsedLifeTime >= lifeTime)
		{
			Kill();
		}

		elapsedLifeTime += Time.deltaTime;
	}

	public void SetLifeTime(float newLifeTime)
	{
		lifeTime = newLifeTime;
	}

	protected virtual void Kill()
	{
		ClearTrails();
		if (networkObject != null && networkObject.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
		{
			networkObject.Despawn();
		}
		else
		{
			PoolManager.Release(gameObject);
		}
	}

	private void RestartTrails()
	{
		if (trailResetRoutine != null)
		{
			StopCoroutine(trailResetRoutine);
		}

		trailResetRoutine = StartCoroutine(ResetTrailsNextFrame());
	}

	private IEnumerator ResetTrailsNextFrame()
	{
		SetTrailsEmitting(false);
		ClearTrails();
		yield return null; // wait one frame so pooled repositioning does not leave a connecting streak
		SetTrailsEmitting(true);
		trailResetRoutine = null;
	}

	protected void ClearTrails()
	{
		foreach (var trail in trailRenderers)
		{
			trail.Clear();
		}
	}

	private void SetTrailsEmitting(bool isEmitting)
	{
		foreach (var trail in trailRenderers)
		{
			trail.emitting = isEmitting;
		}
	}

	public void SetOwner(Character character)
	{
		owner = character;
	}

	public void SetScale(float scaleFactor)
	{
		transform.localScale = baseScale * scaleFactor;
	}
}
