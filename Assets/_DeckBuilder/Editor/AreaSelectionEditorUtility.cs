#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class AreaSelectionEditorUtility
{
    public static ReorderableList CreateControlPointList(SerializedObject owner, SerializedProperty areaProperty)
    {
        if (owner == null || areaProperty == null)
        {
            return null;
        }

        SerializedProperty controlPointsProperty = areaProperty.FindPropertyRelative("controlPoints");
        if (controlPointsProperty == null)
        {
            return null;
        }

        ReorderableList list = new ReorderableList(owner, controlPointsProperty, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Control Points (local X/Z)")
        };

        list.drawElementCallback = (rect, index, active, focused) =>
        {
            SerializedProperty element = controlPointsProperty.GetArrayElementAtIndex(index);
            rect.height = EditorGUIUtility.singleLineHeight;
            element.vector2Value = EditorGUI.Vector2Field(rect, GUIContent.none, element.vector2Value);
        };

        return list;
    }

    public static bool DrawSceneHandles(SerializedProperty areaProperty, Transform ownerTransform)
    {
        if (areaProperty == null || ownerTransform == null)
        {
            return false;
        }

        SerializedProperty controlPointsProperty = areaProperty.FindPropertyRelative("controlPoints");
        if (controlPointsProperty == null || controlPointsProperty.arraySize == 0)
        {
            return false;
        }

        int count = controlPointsProperty.arraySize;
        Vector3[] polygon = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Vector2 local = controlPointsProperty.GetArrayElementAtIndex(i).vector2Value;
            polygon[i] = ownerTransform.TransformPoint(new Vector3(local.x, 0f, local.y));
        }

        Handles.color = new Color(0f, 0.85f, 0.3f, 0.15f);
        for (int i = 1; i < polygon.Length - 1; i++)
        {
            Handles.DrawAAConvexPolygon(polygon[0], polygon[i], polygon[i + 1]);
        }

        Handles.color = Color.green;
        if (polygon.Length > 1)
        {
            Vector3[] loop = new Vector3[polygon.Length + 1];
            polygon.CopyTo(loop, 0);
            loop[loop.Length - 1] = polygon[0];
            Handles.DrawPolyLine(loop);
        }

        bool changed = false;
        Object owner = areaProperty.serializedObject.targetObject;
        for (int i = 0; i < polygon.Length; i++)
        {
            SerializedProperty element = controlPointsProperty.GetArrayElementAtIndex(i);
            Vector3 worldPoint = polygon[i];
            float handleSize = HandleUtility.GetHandleSize(worldPoint) * 0.075f;

            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.FreeMoveHandle(worldPoint, Quaternion.identity, handleSize, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(owner, "Move Area Control Point");
                Vector3 local = ownerTransform.InverseTransformPoint(newWorld);
                element.vector2Value = new Vector2(local.x, local.z);
                changed = true;
            }

            Handles.Label(worldPoint + Vector3.up * 0.2f, $"P{i}");
        }

        return changed;
    }
}
#endif
