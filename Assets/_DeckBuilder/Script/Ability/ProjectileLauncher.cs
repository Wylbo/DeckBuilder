using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
	public bool LaunchProjectile(Projectile projectile)
	{
		return LaunchProjectile(projectile, transform.position);
	}

	public bool LaunchProjectile(Projectile projectile, Vector3 worldPosition)
	{
		Projectile proj = PoolManager.Provide<Projectile>(projectile.gameObject, worldPosition, transform.rotation);

		return proj != null;
	}
}