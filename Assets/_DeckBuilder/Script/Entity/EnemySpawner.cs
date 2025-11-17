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
        var points = controlPointArea?.ControlPoints;
        if (points == null || points.Count < 3)
        {
            return SampleRadiusArea();
        }

        // Build world positions
        int n = points.Count;
        polygonCache.Clear();
        for (int i = 0; i < n; i++)
        {
            polygonCache.Add(transform.TransformPoint(points[i]));
        }

        // Compute plane basis and 2D projection
        if (!TryBuildPlaneBasis(polygonCache, out Vector3 origin, out Vector3 axisX, out Vector3 axisY))
        {
            return SampleRadiusArea();
        }

        var poly2 = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            Vector3 r = polygonCache[i] - origin;
            poly2.Add(new Vector2(Vector3.Dot(r, axisX), Vector3.Dot(r, axisY)));
        }

        List<int> tris = TriangulateConcave2D(poly2);
        if (tris == null || tris.Count < 3)
        {
            return SampleRadiusArea();
        }

        // Accumulate areas in world space
        float totalArea = 0f;
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector3 a = polygonCache[tris[i]];
            Vector3 b = polygonCache[tris[i + 1]];
            Vector3 c = polygonCache[tris[i + 2]];
            totalArea += TriangleArea(a, b, c);
        }
        if (totalArea <= Mathf.Epsilon)
        {
            return SampleRadiusArea();
        }

        float pick = Random.value * totalArea;
        float acc = 0f;
        for (int i = 0; i < tris.Count; i += 3)
        {
            int ia = tris[i];
            int ib = tris[i + 1];
            int ic = tris[i + 2];
            Vector3 a = polygonCache[ia];
            Vector3 b = polygonCache[ib];
            Vector3 c = polygonCache[ic];
            float area = TriangleArea(a, b, c);
            if (area <= Mathf.Epsilon) continue;
            acc += area;
            if (pick <= acc)
            {
                // Sample uniformly in 2D using barycentric, then lift to 3D plane
                Vector2 ua = poly2[ia];
                Vector2 ub = poly2[ib];
                Vector2 uc = poly2[ic];
                Vector2 uv = RandomPointInTriangle2D(ua, ub, uc);
                return origin + axisX * uv.x + axisY * uv.y;
            }
        }

        // Fallback to last triangle center
        int last = tris.Count - 3;
        Vector2 u0 = poly2[tris[last]];
        Vector2 u1 = poly2[tris[last + 1]];
        Vector2 u2 = poly2[tris[last + 2]];
        Vector2 ucent = (u0 + u1 + u2) / 3f;
        return origin + axisX * ucent.x + axisY * ucent.y;
    }

    private static bool TryBuildPlaneBasis(List<Vector3> pts, out Vector3 origin, out Vector3 axisX, out Vector3 axisY)
    {
        origin = Vector3.zero; axisX = Vector3.right; axisY = Vector3.forward;
        if (pts == null || pts.Count < 3) return false;
        // Newell's method for robust polygon normal
        Vector3 normal = Vector3.zero;
        for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i, i++)
        {
            Vector3 pi = pts[i];
            Vector3 pj = pts[j];
            normal.x += (pj.y - pi.y) * (pj.z + pi.z);
            normal.y += (pj.z - pi.z) * (pj.x + pi.x);
            normal.z += (pj.x - pi.x) * (pj.y + pi.y);
        }
        if (normal.sqrMagnitude < 1e-6f)
        {
            normal = Vector3.up; // fallback
        }
        else
        {
            normal.Normalize();
        }
        origin = pts[0];
        // Choose axisX from a non-parallel vector
        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.Cross(normal, Vector3.right);
        tangent.Normalize();
        axisX = tangent;
        axisY = Vector3.Cross(normal, axisX).normalized;
        return true;
    }

    private static List<int> TriangulateConcave2D(IReadOnlyList<Vector2> poly)
    {
        int n = poly.Count; if (n < 3) return null;
        List<int> idx = new List<int>(n); for (int i = 0; i < n; i++) idx.Add(i);
        if (SignedArea2D(poly) < 0f) idx.Reverse();
        List<int> tris = new List<int>(Mathf.Max(0, (n - 2) * 3));
        int guard = 0;
        while (idx.Count > 3 && guard++ < 10000)
        {
            bool earFound = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int i0 = idx[(i + idx.Count - 1) % idx.Count];
                int i1 = idx[i];
                int i2 = idx[(i + 1) % idx.Count];
                if (!IsConvex2D(poly[i0], poly[i1], poly[i2])) continue;
                bool inside = false;
                for (int j = 0; j < idx.Count; j++)
                {
                    int v = idx[j];
                    if (v == i0 || v == i1 || v == i2) continue;
                    if (PointInTriangle2D(poly[v], poly[i0], poly[i1], poly[i2])) { inside = true; break; }
                }
                if (inside) continue;
                tris.Add(i0); tris.Add(i1); tris.Add(i2);
                idx.RemoveAt(i); earFound = true; break;
            }
            if (!earFound) break;
        }
        if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
        return tris;
    }

    private static float SignedArea2D(IReadOnlyList<Vector2> poly)
    {
        float a = 0f; for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i, i++) a += (poly[j].x * poly[i].y - poly[i].x * poly[j].y); return a * 0.5f;
    }
    private static bool IsConvex2D(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 ab = b - a; Vector2 bc = c - b; return (ab.x * bc.y - ab.y * bc.x) > 0f;
    }
    private static bool PointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s = a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y;
        float t = a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y;
        if ((s < 0) != (t < 0)) return false;
        float A = -b.y * c.x + a.y * (c.x - b.x) + a.x * (b.y - c.y) + b.x * c.y;
        if (A < 0) { s = -s; t = -t; A = -A; }
        return s > 0 && t > 0 && (s + t) < A;
    }

    private static Vector2 RandomPointInTriangle2D(Vector2 a, Vector2 b, Vector2 c)
    {
        float r1 = Mathf.Sqrt(Random.value); float r2 = Random.value; return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
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
