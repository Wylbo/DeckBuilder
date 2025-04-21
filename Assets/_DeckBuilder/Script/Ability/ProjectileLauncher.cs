using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{

	public Projectile LaunchProjectile(Projectile projectile, float scaleFactor = 1)
	{
		return LaunchProjectile<Projectile>(projectile, transform.position, scaleFactor);
	}

	public Projectile LaunchProjectile(Projectile projectile, Vector3 worldPos, float scaleFactor = 1)
	{
		return LaunchProjectile<Projectile>(projectile, worldPos, scaleFactor);
	}

	public T LaunchProjectile<T>(Projectile projectile, float scaleFactor = 1) where T : Projectile
	{
		return LaunchProjectile<T>(projectile, transform.position, scaleFactor);
	}

	public T LaunchProjectile<T>(Projectile projectile, Vector3 worldPosition, float scaleFactor = 1) where T : Projectile
	{
		T proj = PoolManager.Provide<T>(projectile.gameObject, worldPosition, transform.rotation);

		IOwnable[] ownables = proj.GetComponentsInChildren<IOwnable>();

		foreach (IOwnable ownable in ownables)
		{
			ownable.SetOwner(GetComponent<Character>());
		}

		proj.SetScale(scaleFactor);

		return proj;
	}
}