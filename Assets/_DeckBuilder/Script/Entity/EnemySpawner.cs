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
        // Use ear clipping triangulation to support concave polygons
        var points = controlPointArea?.ControlPoints;
        if (points == null || points.Count < 3)
        {
            return SampleRadiusArea();
        }

        int n = points.Count;
        // Cache world positions for each vertex once
        if (polygonCache.Capacity < n) polygonCache.Capacity = n;
        polygonCache.Clear();
        for (int i = 0; i < n; i++)
        {
            Vector2 lp = points[i];
            polygonCache.Add(transform.TransformPoint(new Vector3(lp.x, 0f, lp.y)));
        }

        // Triangulate indices
        List<int> tris = TriangulateConcaveXZ(points);
        if (tris == null || tris.Count < 3)
        {
            return SampleRadiusArea();
        }

        // Accumulate triangle areas in world space
        float totalArea = 0f;
        for (int i = 0; i < tris.Count; i += 3)
        {
            totalArea += TriangleArea(polygonCache[tris[i]], polygonCache[tris[i + 1]], polygonCache[tris[i + 2]]);
        }
        if (totalArea <= Mathf.Epsilon)
        {
            return SampleRadiusArea();
        }

        float r = Random.value * totalArea;
        float acc = 0f;
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = polygonCache[tris[i]];
            Vector3 b = polygonCache[tris[i + 1]];
            Vector3 c = polygonCache[tris[i + 2]];
            float area = TriangleArea(a, b, c);
            if (area <= Mathf.Epsilon) continue;
            acc += area;
            if (r <= acc)
            {
                return RandomPointInTriangle(a, b, c);
            }
        }

        // Fallback to last triangle
        int last = tris.Count - 3;
        return RandomPointInTriangle(polygonCache[tris[last]], polygonCache[tris[last + 1]], polygonCache[tris[last + 2]]);
    }

    // Ear clipping triangulation for simple polygons defined in local XZ (Vector2)
    private static List<int> TriangulateConcaveXZ(IReadOnlyList<Vector2> poly)
    {
        int n = poly.Count;
        if (n < 3) return null;

        List<int> indices = new List<int>(n);
        for (int i = 0; i < n; i++) indices.Add(i);

        // Ensure CCW
        if (SignedArea(poly) < 0f)
        {
            indices.Reverse();
        }

        List<int> triangles = new List<int>(Mathf.Max(0, (n - 2) * 3));
        int guard = 0;
        while (indices.Count > 3 && guard < 10000)
        {
            guard++;
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int i0 = indices[(i + indices.Count - 1) % indices.Count];
                int i1 = indices[i];
                int i2 = indices[(i + 1) % indices.Count];

                if (!IsConvex(poly[i0], poly[i1], poly[i2]))
                    continue;

                bool hasPointInside = false;
                for (int j = 0; j < indices.Count; j++)
                {
                    int v = indices[j];
                    if (v == i0 || v == i1 || v == i2) continue;
                    if (PointInTriangle(poly[v], poly[i0], poly[i1], poly[i2]))
                    {
                        hasPointInside = true;
                        break;
                    }
                }
                if (hasPointInside) continue;

                // It's an ear
                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);
                indices.RemoveAt(i);
                earFound = true;
                break;
            }
            if (!earFound)
            {
                // Polygon may be degenerate; break to avoid infinite loop
                break;
            }
        }

        if (indices.Count == 3)
        {
            triangles.Add(indices[0]);
            triangles.Add(indices[1]);
            triangles.Add(indices[2]);
        }

        return triangles;
    }

    private static float SignedArea(IReadOnlyList<Vector2> poly)
    {
        float a = 0f;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i, i++)
        {
            a += (poly[j].x * poly[i].y - poly[i].x * poly[j].y);
        }
        return a * 0.5f;
    }

    private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a;
        Vector2 bc = c - b;
        float cross = ab.x * bc.y - ab.y * bc.x;
        return cross > 0f; // CCW
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s = a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y;
        float t = a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y;
        if ((s < 0) != (t < 0)) return false;
        float A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
        if (A < 0) { s = -s; t = -t; A = -A; }
        return s > 0 && t > 0 && (s + t) < A;
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
        return controlPointArea != null && controlPointArea.HasValidPolygon;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (spawnAreaMode == SpawnAreaMode.ControlPoints && HasValidControlPoints())
        {
            controlPointArea.GetWorldPoints(transform, polygonCache);
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
#if UNITY_EDITOR
            // Draw a filled disk in Scene view to match control area visuals
            UnityEditor.Handles.color = new Color(0f, 0.85f, 0.3f, 0.15f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, radius);
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, radius);
#else
            Gizmos.DrawWireSphere(transform.position, radius);
#endif
        }
    }
}
