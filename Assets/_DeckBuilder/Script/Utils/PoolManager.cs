using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    private class Pool
    {
        public Pool(GameObject prefab, PoolType type)
        {
            Prefab = prefab;
            Type = type;
            Hash = prefab.name.GetHashCode();
            ObjectPool = new ObjectPool<GameObject>(CreateInstance, OnGet, OnRelease, DestroyInstance, false);
        }

        public int Hash { get; }
        public PoolType Type { get; }
        public GameObject Prefab { get; }
        public IObjectPool<GameObject> ObjectPool { get; }

        private GameObject CreateInstance()
        {
            Transform parent = GetParentToSpawnIn(Type);
            GameObject instance = Object.Instantiate(Prefab, parent);
            instance.name = Prefab.name;
            instance.SetActive(false);
            return instance;
        }

        private void OnGet(GameObject obj)
        {
            obj.SetActive(true);
        }

        private void OnRelease(GameObject obj)
        {
            Transform parent = GetParentToSpawnIn(Type);
            obj.transform.SetParent(parent, false);
            obj.SetActive(false);
        }

        private static void DestroyInstance(GameObject obj)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
    }

    public enum PoolType
    {
        VFX,
        GameObject,
    }

    private static PoolManager instance;
    private static readonly Dictionary<PoolType, GameObject> PoolTypeParents = new Dictionary<PoolType, GameObject>();
    private static readonly Dictionary<int, Pool> pools = new Dictionary<int, Pool>();

    private void Awake()
    {
        instance = this;
        PoolTypeParents.Clear();
        foreach (PoolType type in EnumUtils.GetValues<PoolType>())
        {
            GameObject go = new GameObject(type.ToString());
            go.transform.SetParent(transform);
            PoolTypeParents[type] = go;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static T Provide<T>(GameObject obj, Vector3 position, Quaternion rotation, Transform attachedTo = null, PoolType type = PoolType.GameObject)
    {
        GameObject provided = Provide(obj, position, rotation, attachedTo, type);
        return provided != null ? provided.GetComponent<T>() : default;
    }

    public static GameObject Provide(GameObject prefab, Vector3 position, Quaternion rotation, Transform attachedTo = null, PoolType type = PoolType.GameObject)
    {
        if (prefab == null)
        {
            Debug.LogError("Cannot provide from a null prefab");
            return null;
        }

        Pool pool = GetOrCreatePool(prefab, type);
        GameObject instance = pool.ObjectPool.Get();

        Transform parent = attachedTo != null ? attachedTo : GetParentToSpawnIn(type);
        instance.transform.SetParent(parent, false);

        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }

    public static void Release(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return;
        }

        int hash = gameObject.name.GetHashCode();
        if (!pools.TryGetValue(hash, out Pool pool))
        {
            Debug.LogError($"Trying to release an unpooled object {gameObject.name}. Object is destroyed instead");
            Destroy(gameObject);
            return;
        }

        pool.ObjectPool.Release(gameObject);
    }

    private static Pool GetOrCreatePool(GameObject prefab, PoolType type)
    {
        int hash = prefab.name.GetHashCode();
        if (pools.TryGetValue(hash, out Pool pool))
        {
            if (pool.Type != type)
            {
                Debug.LogWarning($"[{nameof(PoolManager)}] Pool for {prefab.name} already exists with type {pool.Type}, requested type {type}");
            }

            return pool;
        }

        pool = new Pool(prefab, type);
        pools.Add(hash, pool);
        return pool;
    }

    private static Transform GetParentToSpawnIn(PoolType type)
    {
        if (PoolTypeParents.TryGetValue(type, out GameObject parent) && parent != null)
        {
            return parent.transform;
        }

        if (instance == null)
        {
            return null;
        }

        GameObject go = new GameObject(type.ToString());
        go.transform.SetParent(instance.transform);
        PoolTypeParents[type] = go;
        return go.transform;
    }
}
