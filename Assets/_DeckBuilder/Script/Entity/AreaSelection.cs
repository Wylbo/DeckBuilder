using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AreaSelection
{
    [SerializeField]
    private List<Vector2> controlPoints = new List<Vector2>();

    [SerializeField, Min(0f)]
    private float height = 2f; // vertical extrusion height (along world Y)

    public IReadOnlyList<Vector2> ControlPoints => controlPoints;
    public float Height { get => height; set => height = Mathf.Max(0f, value); }

    public bool HasValidPolygon => controlPoints != null && controlPoints.Count >= 3;

    public void GetWorldPoints(Transform reference, List<Vector3> results)
    {
        if (results == null) return;
        results.Clear();
        if (reference == null || !HasValidPolygon) return;
        foreach (Vector2 p in controlPoints)
        {
            results.Add(reference.TransformPoint(new Vector3(p.x, 0f, p.y)));
        }
    }

    public Vector3 GetWorldPoint(Transform reference, int index)
    {
        if (reference == null || controlPoints == null || index < 0 || index >= controlPoints.Count) return Vector3.zero;
        Vector2 p = controlPoints[index];
        return reference.TransformPoint(new Vector3(p.x, 0f, p.y));
    }

    public void SetFromWorldPoint(Transform reference, int index, Vector3 worldPosition)
    {
        if (reference == null || controlPoints == null || index < 0 || index >= controlPoints.Count) return;
        Vector3 local = reference.InverseTransformPoint(worldPosition);
        controlPoints[index] = new Vector2(local.x, local.z);
    }

    // Returns true if the given world-space point lies inside the extruded volume (extrusion along reference.up)
    public bool ContainsWorldPoint(Vector3 worldPoint, Transform reference)
    {
        if (!HasValidPolygon || reference == null) return false;

        // Work in reference local space: local.y is along reference.up
        Vector3 local = reference.InverseTransformPoint(worldPoint);
        if (local.y < 0f || local.y > height) return false;
        Vector2 p = new Vector2(local.x, local.z);
        return PointInPolygon2D(controlPoints, p);
    }

    // Closest point on the extruded volume (XZ polygon with Y in [base, base+height])
    public Vector3 ClosestPointOnAreaWorld(Vector3 worldPoint, Transform reference)
    {
        if (reference == null || controlPoints == null || controlPoints.Count == 0) return worldPoint;

        // Work in local space: local.y along up axis
        Vector3 local = reference.InverseTransformPoint(worldPoint);
        Vector2 p = new Vector2(local.x, local.z);

        // If inside horizontally, clamp Y
        if (PointInPolygon2D(controlPoints, p))
        {
            local.y = Mathf.Clamp(local.y, 0f, height);
            return reference.TransformPoint(local);
        }

        // Else, find closest point on polygon edges in local XZ, then convert back to world
        Vector2 best = controlPoints[0];
        float bestSq = float.PositiveInfinity;
        for (int i = 0, j = controlPoints.Count - 1; i < controlPoints.Count; j = i, i++)
        {
            Vector2 a = controlPoints[j];
            Vector2 b = controlPoints[i];
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude;
            float t = denom > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / denom) : 0f;
            Vector2 q = a + t * ab;
            float d = (p - q).sqrMagnitude;
            if (d < bestSq) { bestSq = d; best = q; }
        }
        return reference.TransformPoint(new Vector3(best.x, Mathf.Clamp(local.y, 0f, height), best.y));
    }

    private static bool PointInPolygon2D(IReadOnlyList<Vector2> poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i, i++)
        {
            Vector2 a = poly[i], b = poly[j];
            bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                             (p.x < (b.x - a.x) * (p.y - a.y) / (Mathf.Approximately(b.y - a.y, 0f) ? 1e-6f : (b.y - a.y)) + a.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    // Public API: sample a random point inside the area. If raycastDown is true, casts from top to bottom and returns hit.
    public bool TrySampleWorldPoint(Transform reference, out Vector3 worldPoint, bool raycastDown = true, float raycastPadding = 0.05f, int layerMask = Physics.DefaultRaycastLayers)
    {
        worldPoint = Vector3.zero;
        if (reference == null || !HasValidPolygon) return false;

        int n = controlPoints.Count;
        var poly2 = new List<Vector2>(n);
        for (int i = 0; i < n; i++) poly2.Add(controlPoints[i]);

        List<int> tris = TriangulateConcave2D(poly2);
        if (tris == null || tris.Count < 3) return false;

        float totalArea = 0f;
        for (int i = 0; i < tris.Count; i += 3)
        {
            Vector2 a = poly2[tris[i]];
            Vector2 b = poly2[tris[i + 1]];
            Vector2 c = poly2[tris[i + 2]];
            totalArea += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5f;
        }
        if (totalArea <= Mathf.Epsilon) return false;

        float pick = Random.value * totalArea;
        float acc = 0f;
        for (int i = 0; i < tris.Count; i += 3)
        {
            int ia = tris[i];
            int ib = tris[i + 1];
            int ic = tris[i + 2];
            Vector2 a = poly2[ia];
            Vector2 b = poly2[ib];
            Vector2 c = poly2[ic];
            float area = Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5f;
            if (area <= Mathf.Epsilon) continue;
            acc += area;
            if (pick <= acc)
            {
                Vector2 uv = RandomPointInTriangle2D(a, b, c);
                Vector3 local = new Vector3(uv.x, 0f, uv.y);

                if (raycastDown)
                {
                    float h = Mathf.Max(0f, height);
                    Vector3 topWorld = reference.TransformPoint(new Vector3(local.x, h, local.z));
                    Vector3 bottomWorld = reference.TransformPoint(new Vector3(local.x, 0f, local.z));
                    Vector3 dir = bottomWorld - topWorld;
                    float dist = dir.magnitude;
                    if (dist > 1e-4f)
                    {
                        dir /= dist;
                        if (Physics.Raycast(topWorld, dir, out RaycastHit rh, dist + Mathf.Max(0f, raycastPadding), layerMask))
                        {
                            worldPoint = rh.point;
                            return true;
                        }
                    }
                    worldPoint = bottomWorld;
                    return true;
                }

                worldPoint = reference.TransformPoint(local);
                return true;
            }
        }

        // Fallback to centroid of last triangle
        int last = tris.Count - 3;
        Vector2 la = poly2[tris[last]];
        Vector2 lb = poly2[tris[last + 1]];
        Vector2 lc = poly2[tris[last + 2]];
        Vector2 ucent = (la + lb + lc) / 3f;
        Vector3 loc = new Vector3(ucent.x, 0f, ucent.y);
        if (raycastDown)
        {
            float h2 = Mathf.Max(0f, height);
            Vector3 top = reference.TransformPoint(new Vector3(loc.x, h2, loc.z));
            Vector3 bottom = reference.TransformPoint(new Vector3(loc.x, 0f, loc.z));
            Vector3 d = bottom - top; float L = d.magnitude;
            if (L > 1e-4f)
            {
                d /= L;
                if (Physics.Raycast(top, d, out RaycastHit hit, L + Mathf.Max(0f, raycastPadding), layerMask))
                {
                    worldPoint = hit.point; return true;
                }
            }
            worldPoint = bottom; return true;
        }
        worldPoint = reference.TransformPoint(loc); return true;
    }

    // Internal helpers for triangulation/sampling in local XZ
    private static Vector2 RandomPointInTriangle2D(Vector2 a, Vector2 b, Vector2 c)
    {
        float r1 = Mathf.Sqrt(Random.value);
        float r2 = Random.value;
        return (1 - r1) * a + (r1 * (1 - r2)) * b + (r1 * r2) * c;
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
}
