using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
	private Projectile projectile;
	private Vector3 worldPosition;
	private Quaternion rotation;
	private float scaleFactor = 1;
	private Character owner;

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

	public T Launch<T>() where T : Projectile
	{
		if (projectile == null)
		{
			Debug.LogError("Projectile is not set!");
			return null;
		}

		T proj = PoolManager.Provide<T>(projectile.gameObject, worldPosition, rotation);

		IOwnable[] ownables = proj.GetComponentsInChildren<IOwnable>();
		foreach (IOwnable ownable in ownables)
		{
			ownable.SetOwner(owner);
		}

		proj.SetScale(scaleFactor);

		Reset();

		return proj;
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