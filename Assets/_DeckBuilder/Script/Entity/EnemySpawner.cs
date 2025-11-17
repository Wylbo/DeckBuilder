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
        [Range(0f, 100f)]
        public float percentage;
    }

    [Header("Spawn Setup")]
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private SpawnableEnemy[] spawnableEnemies;
    [SerializeField] private bool spawnOnStart;
    [SerializeField, Min(0)] private int spawnCountOnStart = 1;

    [Header("Area Settings")]
    [SerializeField] private SpawnAreaMode spawnAreaMode = SpawnAreaMode.Radius;
    [SerializeField, Min(0.1f)] private float radius = 5f;
    [SerializeField] private SpawnAreaDefinition spawnAreaDefinition;

    [Header("NavMesh Settings")]
    [SerializeField, Min(1)] private int maxAttemptsPerSpawn = 10;
    [SerializeField, Min(0.1f)] private float navMeshSampleDistance = 2f;
    [SerializeField] private int navMeshAreaMask = NavMesh.AllAreas;

    private readonly List<Vector3> polygonCache = new List<Vector3>();

    private void Awake()
    {
        if (enemyManager == null)
        {
            enemyManager = FindObjectOfType<EnemyManager>();
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

        Character instance = Instantiate(prefab, spawnPosition, Quaternion.identity);
        InitializeSpawnedEnemy(instance);
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
            if (entry.prefab == null || entry.percentage <= 0f)
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
            if (entry.prefab == null || entry.percentage <= 0f)
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

    private void InitializeSpawnedEnemy(Character character)
    {
        if (character == null)
        {
            return;
        }

        enemyManager?.Register(character);
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
        spawnAreaDefinition?.GetWorldPoints(transform, polygonCache);

        if (polygonCache.Count < 3)
        {
            return SampleRadiusArea();
        }

        float totalArea = 0f;
        for (int i = 1; i < polygonCache.Count - 1; i++)
        {
            totalArea += TriangleArea(polygonCache[0], polygonCache[i], polygonCache[i + 1]);
        }

        if (totalArea <= Mathf.Epsilon)
        {
            return SampleRadiusArea();
        }

        float randomArea = Random.value * totalArea;
        float accumulatedArea = 0f;

        for (int i = 1; i < polygonCache.Count - 1; i++)
        {
            Vector3 a = polygonCache[0];
            Vector3 b = polygonCache[i];
            Vector3 c = polygonCache[i + 1];

            float area = TriangleArea(a, b, c);
            if (area <= Mathf.Epsilon)
            {
                continue;
            }

            accumulatedArea += area;
            if (randomArea <= accumulatedArea)
            {
                return RandomPointInTriangle(a, b, c);
            }
        }

        return RandomPointInTriangle(polygonCache[0], polygonCache[polygonCache.Count - 2], polygonCache[polygonCache.Count - 1]);
    }

    private static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        return Vector3.Cross(ab, ac).magnitude * 0.5f;
    }

    private static Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
    }

    private bool HasValidControlPoints()
    {
        return spawnAreaDefinition != null && spawnAreaDefinition.HasValidPolygon;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (spawnAreaMode == SpawnAreaMode.ControlPoints && HasValidControlPoints())
        {
            spawnAreaDefinition.GetWorldPoints(transform, polygonCache);
            if (polygonCache.Count == 0)
            {
                return;
            }

            Vector3 previous = polygonCache[0];
            Gizmos.DrawSphere(previous, 0.2f);

            for (int i = 1; i < polygonCache.Count; i++)
            {
                Vector3 point = polygonCache[i];
                Gizmos.DrawSphere(point, 0.2f);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            Gizmos.DrawLine(previous, polygonCache[0]);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
