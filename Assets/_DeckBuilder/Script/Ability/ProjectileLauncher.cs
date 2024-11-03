using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{

	public Projectile LaunchProjectile(Projectile projectile)
	{
		return LaunchProjectile<Projectile>(projectile, transform.position);
	}

	public Projectile LaunchProjectile(Projectile projectile, Vector3 worldPos)
	{
		return LaunchProjectile<Projectile>(projectile, worldPos);
	}

	public T LaunchProjectile<T>(Projectile projectile)
	{
		return LaunchProjectile<T>(projectile, transform.position);
	}

	public T LaunchProjectile<T>(Projectile projectile, Vector3 worldPosition)
	{
		T proj = PoolManager.Provide<T>(projectile.gameObject, worldPosition, transform.rotation);

		return proj;
	}
}