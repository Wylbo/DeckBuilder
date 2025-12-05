using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class MinimapMeshGenerator : MonoBehaviour
{
    [Button]
    void GenerateMesh()
    {
        MeshFilter meshFileter = GetComponent<MeshFilter>();

        NavMeshTriangulation triangles = NavMesh.CalculateTriangulation();

        Mesh mesh = new Mesh();
        mesh.vertices = triangles.vertices;
        mesh.triangles = triangles.indices;

        meshFileter.mesh = mesh;
    }
}
