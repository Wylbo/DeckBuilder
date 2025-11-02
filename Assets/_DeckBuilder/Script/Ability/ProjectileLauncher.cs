using UnityEditor.Rendering;
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
	public enum MultipleProjectileFireMode
	{
		Fan,
	}

	private Projectile projectile;
	private Vector3 worldPosition;
	private Quaternion rotation;
	private float scaleFactor = 1;
	private Character owner;
	private int projectileCount = 1;
	private MultipleProjectileFireMode multipleProjectileFireMode = MultipleProjectileFireMode.Fan;
	private float spreadAngle;
	private float maxSpreadAngle;

	public ProjectileLauncher SetProjectile(Projectile projectile)
	{
		this.projectile = projectile;
		return this;
	}

	public ProjectileLauncher SetPosition(Vector3 position)
	{
		this.worldPosition = position;
		return this;
	}

	public ProjectileLauncher AtCasterPosition()
	{
		this.worldPosition = transform.position;
		return this;
	}

	public ProjectileLauncher SetRotation(Quaternion rotation)
	{
		this.rotation = rotation;
		return this;
	}

	public ProjectileLauncher SetScale(float scaleFactor)
	{
		this.scaleFactor = scaleFactor;
		return this;
	}

	public ProjectileLauncher SetOwner(Character owner)
	{
		this.owner = owner;
		return this;
	}

	public ProjectileLauncher SetProjectileCount(int count)
	{
		this.projectileCount = count;
		return this;
	}

	public ProjectileLauncher SetSpreadAngle(float angle)
	{
		spreadAngle = angle;
		return this;
	}

	public ProjectileLauncher SetMaxSpreadAngle(float angle)
	{
		maxSpreadAngle = angle;
		return this;
	}

	public ProjectileLauncher SetMultipleProjectileFireMode(MultipleProjectileFireMode mode)
	{
		multipleProjectileFireMode = mode;
		return this;
	}


	public T[] Launch<T>() where T : Projectile
	{
		if (projectile == null)
		{
			Debug.LogError("Projectile is not set!");
			return null;
		}

		int count = Mathf.Max(0, projectileCount);
		if (count == 0)
			return new T[0];

		var results = new T[count];

		var prefab = projectile.gameObject;
		var baseRot = rotation;
		var position = worldPosition;
		var scale = scaleFactor;

		bool isFan = count > 1 && multipleProjectileFireMode == MultipleProjectileFireMode.Fan;

		float adjustedSpreadAngle = spreadAngle;
		float totalSpread = spreadAngle * (count - 1);
		if (isFan && totalSpread > maxSpreadAngle)
		{
			totalSpread = maxSpreadAngle;
			adjustedSpreadAngle = totalSpread / count;
		}

		float startAngle = isFan ? -totalSpread * 0.5f : 0f;

		for (int i = 0; i < count; i++)
		{
			Quaternion shotRot = isFan
				? Quaternion.Euler(0f, startAngle + adjustedSpreadAngle * i, 0f) * baseRot
				: baseRot;

			var proj = PoolManager.Provide<T>(prefab, position, shotRot);
			results[i] = proj;

			var ownables = proj.GetComponentsInChildren<IOwnable>();
			for (int j = 0; j < ownables.Length; j++)
				ownables[j].SetOwner(owner);

			proj.SetScale(scale);
		}

		Reset();
		return results;
	}

	private void Reset()
	{
		projectile = null;
		worldPosition = transform.position;
		rotation = transform.rotation;
		scaleFactor = 1;
		owner = GetComponent<Character>();
	}

	private void Awake()
	{
		Reset();
	}
}