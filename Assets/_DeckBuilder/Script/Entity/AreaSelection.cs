using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AreaSelection
{
    [SerializeField]
    private List<Vector2> controlPoints = new List<Vector2>();

    public IReadOnlyList<Vector2> ControlPoints => controlPoints;

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

        foreach (Vector2 point in controlPoints)
        {
            Vector3 localPoint = new Vector3(point.x, 0f, point.y);
            Vector3 worldPoint = reference.TransformPoint(localPoint);
            results.Add(worldPoint);
        }
    }

    public Vector3 GetWorldPoint(Transform reference, int index)
    {
        if (reference == null || controlPoints == null || index < 0 || index >= controlPoints.Count)
        {
            return Vector3.zero;
        }

        Vector2 point = controlPoints[index];
        return reference.TransformPoint(new Vector3(point.x, 0f, point.y));
    }

    public void SetFromWorldPoint(Transform reference, int index, Vector3 worldPosition)
    {
        if (reference == null || controlPoints == null || index < 0 || index >= controlPoints.Count)
        {
            return;
        }

        Vector3 localPoint = reference.InverseTransformPoint(worldPosition);
        controlPoints[index] = new Vector2(localPoint.x, localPoint.z);
    }
}
