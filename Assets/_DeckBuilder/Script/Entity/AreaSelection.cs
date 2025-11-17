using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AreaSelection
{
    [SerializeField]
    private List<Vector3> controlPoints = new List<Vector3>();

    public IReadOnlyList<Vector3> ControlPoints => controlPoints;

    public bool HasValidPolygon => controlPoints != null && controlPoints.Count >= 3;

    public void GetWorldPoints(Transform reference, List<Vector3> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();

        if (reference == null || !HasValidPolygon)
        {
            return;
        }

        foreach (Vector3 point in controlPoints)
        {
            Vector3 worldPoint = reference.TransformPoint(point);
            results.Add(worldPoint);
        }
    }

    public Vector3 GetWorldPoint(Transform reference, int index)
    {
        if (reference == null || controlPoints == null || index < 0 || index >= controlPoints.Count)
        {
            return Vector3.zero;
        }

        Vector3 point = controlPoints[index];
        return reference.TransformPoint(point);
    }

    public void SetFromWorldPoint(Transform reference, int index, Vector3 worldPosition)
    {
        if (reference == null || controlPoints == null || index < 0 || index >= controlPoints.Count)
        {
            return;
        }

        Vector3 localPoint = reference.InverseTransformPoint(worldPosition);
        controlPoints[index] = localPoint;
    }

    // Returns true if the given world-space point lies inside the polygon defined by controlPoints
    // Supports concave shapes and arbitrary inclination (projects onto the polygon plane)
    public bool ContainsWorldPoint(Vector3 worldPoint, Transform reference)
    {
        if (!HasValidPolygon || reference == null) return false;

        var world = new List<Vector3>(controlPoints.Count);
        foreach (var lp in controlPoints)
        {
            world.Add(reference.TransformPoint(lp));
        }

        if (!TryBuildPlaneBasis(world, out var origin, out var axisX, out var axisY, out var normal))
        {
            return false;
        }

        var poly2 = new List<Vector2>(world.Count);
        for (int i = 0; i < world.Count; i++)
        {
            Vector3 r = world[i] - origin;
            poly2.Add(new Vector2(Vector3.Dot(r, axisX), Vector3.Dot(r, axisY)));
        }

        Vector3 rp = worldPoint - origin;
        Vector2 p2 = new Vector2(Vector3.Dot(rp, axisX), Vector3.Dot(rp, axisY));
        return PointInPolygon2D(poly2, p2);
    }

    // Returns the closest point on the polygon in world space to the given world point.
    // If inside, returns the projection onto the polygon plane; otherwise the closest point on any polygon edge.
    public Vector3 ClosestPointOnAreaWorld(Vector3 worldPoint, Transform reference)
    {
        if (reference == null || controlPoints == null || controlPoints.Count == 0)
        {
            return worldPoint;
        }

        var world = new List<Vector3>(controlPoints.Count);
        foreach (var lp in controlPoints)
        {
            world.Add(reference.TransformPoint(lp));
        }

        if (controlPoints.Count < 3)
        {
            // Fallback to nearest vertex
            Vector3 best = world[0];
            float bestSq = (worldPoint - best).sqrMagnitude;
            for (int i = 1; i < world.Count; i++)
            {
                float d = (worldPoint - world[i]).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = world[i]; }
            }
            return best;
        }

        if (!TryBuildPlaneBasis(world, out var origin, out var axisX, out var axisY, out var normal))
        {
            return world[0];
        }

        // Project onto plane
        Plane plane = new Plane(normal, origin);
        Vector3 onPlane = plane.ClosestPointOnPlane(worldPoint);

        // If inside, the projected point is closest
        var poly2 = new List<Vector2>(world.Count);
        for (int i = 0; i < world.Count; i++)
        {
            Vector3 r = world[i] - origin;
            poly2.Add(new Vector2(Vector3.Dot(r, axisX), Vector3.Dot(r, axisY)));
        }
        Vector3 rp = onPlane - origin;
        Vector2 p2 = new Vector2(Vector3.Dot(rp, axisX), Vector3.Dot(rp, axisY));
        if (PointInPolygon2D(poly2, p2)) return onPlane;

        // Else closest on edges (3D)
        Vector3 bestPoint = world[0];
        float bestDistSq = float.PositiveInfinity;
        for (int i = 0, j = world.Count - 1; i < world.Count; j = i, i++)
        {
            Vector3 a = world[j], b = world[i];
            Vector3 ab = b - a;
            float denom = ab.sqrMagnitude;
            float t = denom > 1e-6f ? Mathf.Clamp01(Vector3.Dot(worldPoint - a, ab) / denom) : 0f;
            Vector3 q = a + t * ab;
            float d = (worldPoint - q).sqrMagnitude;
            if (d < bestDistSq) { bestDistSq = d; bestPoint = q; }
        }
        return bestPoint;
    }

    private static bool TryBuildPlaneBasis(IReadOnlyList<Vector3> pts, out Vector3 origin, out Vector3 axisX, out Vector3 axisY, out Vector3 normal)
    {
        origin = Vector3.zero; axisX = Vector3.right; axisY = Vector3.forward; normal = Vector3.up;
        if (pts == null || pts.Count < 3) return false;
        // Newell's method for robust normal
        Vector3 n = Vector3.zero;
        for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i, i++)
        {
            Vector3 pi = pts[i], pj = pts[j];
            n.x += (pj.y - pi.y) * (pj.z + pi.z);
            n.y += (pj.z - pi.z) * (pj.x + pi.x);
            n.z += (pj.x - pi.x) * (pj.y + pi.y);
        }
        if (n.sqrMagnitude < 1e-6f) n = Vector3.up; else n.Normalize();
        normal = n;
        origin = pts[0];
        Vector3 tangent = Vector3.Cross(n, Vector3.up);
        if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.Cross(n, Vector3.right);
        tangent.Normalize();
        axisX = tangent;
        axisY = Vector3.Cross(n, axisX).normalized;
        return true;
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
