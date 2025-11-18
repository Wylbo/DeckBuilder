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
}
