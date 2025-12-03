using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    private enum SpawnAreaMode
    {
        Radius,
        ControlPoints
    }

    [System.Serializable]
    private struct SpawnableEnemy
    {
        public Character prefab;
        [Range(0, 100)]
        public int percentage;
    }

    [Header("Spawn Setup")]
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private SpawnableEnemy[] spawnableEnemies;
    [SerializeField] private bool spawnOnStart;
    [SerializeField, Min(0)] private int spawnCountOnStart = 1;

    [Header("Area Settings")]
    [SerializeField] private SpawnAreaMode spawnAreaMode = SpawnAreaMode.Radius;
    [SerializeField, Min(0.1f)] private float radius = 5f;
    [SerializeField] private AreaSelection controlPointArea = new AreaSelection();

    [Header("NavMesh Settings")]
    [SerializeField, Min(1)] private int maxAttemptsPerSpawn = 10;
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 2f;
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

    private readonly List<Vector3> polygonCache = new List<Vector3>();

    private void Awake()
    {
        if (enemyManager == null)
        {
            enemyManager = FindFirstObjectByType<EnemyManager>();
        }
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemies(spawnCountOnStart);
        }
    }

    public void SpawnEnemies(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            SpawnRandomEnemy();
        }
    }

    public Character SpawnRandomEnemy()
    {
        Character prefab = GetWeightedRandomPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] No spawnable prefabs configured", this);
            return null;
        }

        return SpawnEnemy(prefab);
    }

    public AreaSelection ControlPointArea => controlPointArea;

    public Character SpawnEnemy(Character prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] Cannot spawn a null prefab", this);
            return null;
        }

        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] Failed to find a valid spawn position", this);
            return null;
        }

        Character instance = SpawnFromManager(prefab, spawnPosition);
        return instance;
    }

    private Character GetWeightedRandomPrefab()
    {
        if (spawnableEnemies == null || spawnableEnemies.Length == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        foreach (SpawnableEnemy entry in spawnableEnemies)
        {
            if (entry.prefab == null || entry.percentage <= 0)
            {
                continue;
            }

            totalWeight += entry.percentage;
        }

        if (totalWeight <= 0f)
        {
            foreach (SpawnableEnemy entry in spawnableEnemies)
            {
                if (entry.prefab != null)
                {
                    return entry.prefab;
                }
            }

            return null;
        }

        float roll = Random.value * totalWeight;
        foreach (SpawnableEnemy entry in spawnableEnemies)
        {
            if (entry.prefab == null || entry.percentage <= 0)
            {
                continue;
            }

            roll -= entry.percentage;
            if (roll <= 0f)
            {
                return entry.prefab;
            }
        }

        return null;
    }

    private Character SpawnFromManager(Character prefab, Vector3 spawnPosition)
    {
        Character instance = null;

        if (enemyManager != null)
        {
            instance = enemyManager.SpawnFromPool(prefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            GameObject provided = PoolManager.Provide(prefab.gameObject, spawnPosition, Quaternion.identity, transform);
            if (provided != null)
            {
                instance = provided.GetComponent<Character>();
                if (instance == null)
                {
                    Debug.LogError($"[{nameof(EnemySpawner)}] Spawned object {provided.name} is missing a {nameof(Character)} component", this);
                }
                else
                {
                    Debug.LogWarning($"[{nameof(EnemySpawner)}] No {nameof(EnemyManager)} assigned, spawned enemy will not be tracked", this);
                }
            }
        }

        return instance;
    }

    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            Vector3 candidate = spawnAreaMode == SpawnAreaMode.ControlPoints && HasValidControlPoints()
                ? SampleControlPointArea()
                : SampleRadiusArea();

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleDistance, navMeshAreaMask))
            {
                spawnPosition = hit.position;
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private Vector3 SampleRadiusArea()
    {
        Vector2 randomCircle = Random.insideUnitCircle * radius;
        Vector3 basePosition = transform.position;
        return new Vector3(basePosition.x + randomCircle.x, basePosition.y, basePosition.z + randomCircle.y);
    }

    private Vector3 SampleControlPointArea()
    {
        if (controlPointArea != null && controlPointArea.TrySampleWorldPoint(transform, out var worldPoint, true))
        {
            return worldPoint;
        }
        return SampleRadiusArea();
    }

    // Geometry helpers moved into AreaSelection

    private bool HasValidControlPoints()
    {
        return controlPointArea != null && controlPointArea.HasValidPolygon;
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        Gizmos.color = Color.green;
        if (spawnAreaMode == SpawnAreaMode.Radius)
        {
            UnityEditor.Handles.color = new Color(0f, 0.85f, 0.3f, 0.15f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, radius);
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, radius);
        }
#endif
    }
}
