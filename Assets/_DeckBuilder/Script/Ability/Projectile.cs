using UnityEngine;

public class Projectile : MonoBehaviour
{
	[SerializeField]
	protected float lifeTime = 0.0f;

	protected float elapsedLifeTime = 0.0f;

	protected float TimeRatio => lifeTime > 0 ? Mathf.Clamp01(elapsedLifeTime / lifeTime) : 0;

	protected virtual void OnEnable()
	{
		elapsedLifeTime = 0;
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
		PoolManager.Release(gameObject);
	}
}
