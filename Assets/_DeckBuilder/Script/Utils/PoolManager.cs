using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
	private class Pool
	{
		public int Hash { get; private set; }

		public List<GameObject> AvailableObject { get; set; }

		public Pool(int hash)
		{
			Hash = hash;
			AvailableObject = new List<GameObject>();
		}
	}


	public enum PoolType
	{
		VFX,
		GameObject,
	}

	private static Dictionary<PoolType, GameObject> PoolTypeParents = new Dictionary<PoolType, GameObject>();
	private static List<Pool> pools = new List<Pool>();

	private void Awake()
	{
		PoolTypeParents = new Dictionary<PoolType, GameObject>();
		GameObject go;
		foreach (PoolType type in EnumUtils.GetValues<PoolType>())
		{
			go = new GameObject(type.ToString());
			PoolTypeParents.Add(type, go);
			go.transform.parent = transform;
		}
	}

	public static T Provide<T>(GameObject obj, Vector3 position, Quaternion rotation, PoolType type = PoolType.GameObject)
	{
		return Provide(obj, position, rotation, type).GetComponent<T>();
	}

	public static GameObject Provide(GameObject gameObject, Vector3 position, Quaternion rotation, PoolType type = PoolType.GameObject)
	{
		Pool pool = pools.Find(p => p.Hash == gameObject.name.GetHashCode());

		if (pool == null)
		{
			pool = new Pool(gameObject.name.GetHashCode());
			pools.Add(pool);
		}

		GameObject providedObj = pool.AvailableObject.FirstOrDefault();

		if (providedObj == null)
		{
			providedObj = Instantiate(gameObject, position, rotation, GetParentToSpawnIn(type));
			providedObj.name = gameObject.name;
		}
		else
		{
			providedObj.transform.SetPositionAndRotation(position, rotation);
			providedObj.SetActive(true);

			pool.AvailableObject.Remove(providedObj);
		}

		return providedObj;
	}

	public static void Release(GameObject gameObject)
	{
		Pool pool = pools.Find(p => p.Hash == gameObject.name.GetHashCode());

		if (pool == null)
		{
			Debug.LogError($"Trying to release an unpooled object {gameObject.name}. Object is destroyed instead");
			Destroy(gameObject);
			return;
		}

		gameObject.SetActive(false);
		pool.AvailableObject.Add(gameObject);
	}

	private static Transform GetParentToSpawnIn(PoolType type)
	{
		if (PoolTypeParents.TryGetValue(type, out GameObject parent))
		{
			return parent.transform;
		}

		return null;

	}
}
