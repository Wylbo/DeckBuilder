using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
	public enum MultipleProjectileFireMode
	{
		Fan,
		Scatter
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

		int count = Mathf.Max(1, projectileCount);
		if (count == 0)
			return new T[0];

		var results = new T[count];

		// float adjustedSpreadAngle = CalculateAdjustedSpreadAngle(count);
		// float startAngle = CalculateStartAngle(count, adjustedSpreadAngle);
		float startAngle, stepAngle;
		GetFanStartAndStep(count, out startAngle, out stepAngle);
		for (int i = 0; i < count; i++)
		{
			results[i] = LaunchProjectile<T>(i, startAngle, stepAngle);
		}

		Reset();
		return results;
	}

	private void GetFanStartAndStep(int count, out float startAngle, out float stepAngle)
	{
		startAngle = 0f;
		stepAngle = 0f;

		if (multipleProjectileFireMode != MultipleProjectileFireMode.Fan || count <= 1)
			return;

		float desiredTotal = spreadAngle * (count - 1);
		float cap = (maxSpreadAngle > 0f) ? Mathf.Min(desiredTotal, maxSpreadAngle) : desiredTotal;

		// Full circle? -> ring distribution
		const float EPS = 0.001f;
		if (cap >= 360f - EPS)
		{
			stepAngle = 360f / count;
			startAngle = 0f;
			return;
		}


		stepAngle = (cap > 0f) ? cap / (count - 1) : 0f;
		startAngle = -0.5f * cap;
	}
	private T LaunchProjectile<T>(int index, float startAngle, float adjustedSpreadAngle) where T : Projectile
	{
		Quaternion shotRot = CalculateProjectileRotation(index, startAngle, adjustedSpreadAngle);
		Vector3 shotPosition = CalculateProjectilePosition(index);

		var proj = PoolManager.Provide<T>(projectile.gameObject, shotPosition, shotRot);

		AssignOwner(proj);
		proj.SetScale(scaleFactor);

		return proj;
	}

	private Quaternion CalculateProjectileRotation(int index, float startAngle, float adjustedSpreadAngle)
	{
		if (multipleProjectileFireMode == MultipleProjectileFireMode.Fan)
		{
			float angle = startAngle + adjustedSpreadAngle * index;
			return Quaternion.Euler(0f, angle, 0f) * rotation;
		}

		return rotation;
	}

	private Vector3 CalculateProjectilePosition(int index)
	{
		if (multipleProjectileFireMode == MultipleProjectileFireMode.Scatter && projectileCount > 1)
		{
			float radius = Random.Range(0f, maxSpreadAngle);
			float angle = Random.Range(0f, 360f);
			float x = worldPosition.x + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
			float z = worldPosition.z + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
			return new Vector3(x, worldPosition.y, z);
		}

		// For Fan mode, use the world position
		return worldPosition;
	}

	private void AssignOwner(Projectile proj)
	{
		var ownables = proj.GetComponentsInChildren<IOwnable>();
		foreach (var ownable in ownables)
		{
			ownable.SetOwner(owner);
		}
	}

	private void Reset()
	{
		projectile = null;
		worldPosition = transform.position;
		rotation = transform.rotation;
		scaleFactor = 1;
		owner = GetComponent<Character>();
		projectileCount = 1;
		multipleProjectileFireMode = MultipleProjectileFireMode.Fan;
		spreadAngle = 0f;
		maxSpreadAngle = 0f;
	}

	private void Awake()
	{
		Reset();
	}
}