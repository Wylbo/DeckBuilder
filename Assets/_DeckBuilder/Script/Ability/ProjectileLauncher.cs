using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
	public bool LaunchProjectile(Projectile projectile)
	{
		Projectile proj = PoolManager.Provide<Projectile>(projectile.gameObject, transform.position, transform.rotation);

		return proj != null;
	}
}