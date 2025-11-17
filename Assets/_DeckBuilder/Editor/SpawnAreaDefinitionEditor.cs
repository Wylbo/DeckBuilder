#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(SpawnAreaDefinition))]
public class SpawnAreaDefinitionEditor : Editor
{
    private SerializedProperty controlPointsProperty;
    private ReorderableList controlPointsList;

    private void OnEnable()
    {
        controlPointsProperty = serializedObject.FindProperty("controlPoints");

        controlPointsList = new ReorderableList(serializedObject, controlPointsProperty, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Control Points (local X/Z)")
        };

        controlPointsList.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty element = controlPointsProperty.GetArrayElementAtIndex(index);
            rect.height = EditorGUIUtility.singleLineHeight;
            element.vector2Value = EditorGUI.Vector2Field(rect, GUIContent.none, element.vector2Value);
        };

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("Define polygon control points in local space (X/Z). This asset can be reused across multiple spawners.", MessageType.Info);

        controlPointsList.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (Selection.activeObject != target)
        {
            return;
        }

        serializedObject.Update();
        bool changed = DrawSceneHandles();

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
    }

    private bool DrawSceneHandles()
    {
        if (controlPointsProperty == null || controlPointsProperty.arraySize == 0)
        {
            return false;
        }

        List<Vector3> polygon = new List<Vector3>(controlPointsProperty.arraySize);
        for (int i = 0; i < controlPointsProperty.arraySize; i++)
        {
            Vector2 value = controlPointsProperty.GetArrayElementAtIndex(i).vector2Value;
            polygon.Add(new Vector3(value.x, 0f, value.y));
        }

        DrawSurfaceFill(polygon);

        Handles.color = Color.green;
        Vector3[] loop = ClosePolygonLoop(polygon);
        if (loop.Length >= 2)
        {
            Handles.DrawPolyLine(loop);
        }

        bool changed = false;
        for (int i = 0; i < polygon.Count; i++)
        {
            SerializedProperty element = controlPointsProperty.GetArrayElementAtIndex(i);
            Vector3 position = polygon[i];
            float handleSize = HandleUtility.GetHandleSize(position) * 0.075f;

            EditorGUI.BeginChangeCheck();
            var fmh_96_68_638990098903205293 = Quaternion.identity; Vector3 newPosition = Handles.FreeMoveHandle(position, handleSize, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Move Spawn Area Control Point");
                Vector3 flattened = new Vector3(newPosition.x, 0f, newPosition.z);
                element.vector2Value = new Vector2(flattened.x, flattened.z);
                changed = true;
            }

            Handles.Label(position + Vector3.up * 0.2f, $"P{i}");
        }

        return changed;
    }

    private void DrawSurfaceFill(List<Vector3> polygon)
    {
        if (polygon == null || polygon.Count < 3)
        {
            return;
        }

        Handles.color = new Color(0f, 0.85f, 0.3f, 0.15f);
        for (int i = 1; i < polygon.Count - 1; i++)
        {
            Handles.DrawAAConvexPolygon(polygon[0], polygon[i], polygon[i + 1]);
        }
    }

    private Vector3[] ClosePolygonLoop(List<Vector3> polygon)
    {
        if (polygon == null || polygon.Count == 0)
        {
            return System.Array.Empty<Vector3>();
        }

        Vector3[] loop = new Vector3[polygon.Count + 1];
        for (int i = 0; i < polygon.Count; i++)
        {
            loop[i] = polygon[i];
        }

        loop[loop.Length - 1] = polygon[0];
        return loop;
    }
}
#endif
