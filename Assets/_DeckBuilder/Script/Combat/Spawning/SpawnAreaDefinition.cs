using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DeckBuilder/Spawning/Spawn Area Definition", fileName = "SpawnAreaDefinition")]
public class SpawnAreaDefinition : ScriptableObject
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
}
