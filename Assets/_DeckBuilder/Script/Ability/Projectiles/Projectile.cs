using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour, IOwnable
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

	private void Awake()
	{
		baseScale = transform.localScale;
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
		PoolManager.Release(gameObject);
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
