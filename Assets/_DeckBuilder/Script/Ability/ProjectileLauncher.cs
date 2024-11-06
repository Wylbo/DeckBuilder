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

	public T LaunchProjectile<T>(Projectile projectile) where T : MonoBehaviour
	{
		return LaunchProjectile<T>(projectile, transform.position);
	}

	public T LaunchProjectile<T>(Projectile projectile, Vector3 worldPosition) where T : MonoBehaviour
	{
		T proj = PoolManager.Provide<T>(projectile.gameObject, worldPosition, transform.rotation);

		IOwnable[] ownables = proj.GetComponentsInChildren<IOwnable>();

		foreach (IOwnable ownable in ownables)
		{
			ownable.SetOwner(GetComponent<Character>());
		}

		return proj;
	}
}