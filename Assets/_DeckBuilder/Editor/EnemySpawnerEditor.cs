#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    private SerializedProperty enemyManagerProperty;
    private SerializedProperty spawnableEnemiesProperty;
    private SerializedProperty spawnOnStartProperty;
    private SerializedProperty spawnCountOnStartProperty;
    private SerializedProperty spawnAreaModeProperty;
    private SerializedProperty radiusProperty;
    private SerializedProperty controlPointAreaProperty;
    private SerializedProperty controlPointsProperty;
    private SerializedProperty maxAttemptsProperty;
    private SerializedProperty navSampleDistanceProperty;
    private SerializedProperty navAreaMaskProperty;

    private ReorderableList spawnableEnemiesList;
    private ReorderableList controlPointsList;

    private void OnEnable()
    {
        enemyManagerProperty = serializedObject.FindProperty("enemyManager");
        spawnableEnemiesProperty = serializedObject.FindProperty("spawnableEnemies");
        spawnOnStartProperty = serializedObject.FindProperty("spawnOnStart");
        spawnCountOnStartProperty = serializedObject.FindProperty("spawnCountOnStart");
        spawnAreaModeProperty = serializedObject.FindProperty("spawnAreaMode");
        radiusProperty = serializedObject.FindProperty("radius");
        controlPointAreaProperty = serializedObject.FindProperty("controlPointArea");
        controlPointsProperty = controlPointAreaProperty?.FindPropertyRelative("controlPoints");
        maxAttemptsProperty = serializedObject.FindProperty("maxAttemptsPerSpawn");
        navSampleDistanceProperty = serializedObject.FindProperty("navMeshSampleDistance");
        navAreaMaskProperty = serializedObject.FindProperty("navMeshAreaMask");

        spawnableEnemiesList = new ReorderableList(serializedObject, spawnableEnemiesProperty, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawnable Enemies"),
            elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
        };

        spawnableEnemiesList.drawElementCallback = DrawSpawnableEnemyElement;

        if (controlPointsProperty != null)
        {
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
        }

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Spawn Setup", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enemyManagerProperty);
        spawnableEnemiesList.DoLayoutList();
        DrawSpawnPercentageSummary();
        EditorGUILayout.PropertyField(spawnOnStartProperty);
        EditorGUILayout.PropertyField(spawnCountOnStartProperty);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Area Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnAreaModeProperty);
        if (spawnAreaModeProperty.enumValueIndex == 0)
        {
            EditorGUILayout.PropertyField(radiusProperty);
        }
        else
        {
            if (controlPointsList != null)
            {
                controlPointsList.DoLayoutList();
            }
            else
            {
                EditorGUILayout.HelpBox("Control points list is unavailable.", MessageType.Info);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("NavMesh Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxAttemptsProperty);
        EditorGUILayout.PropertyField(navSampleDistanceProperty);
        EditorGUILayout.PropertyField(navAreaMaskProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSpawnableEnemyElement(Rect rect, int index, bool active, bool focused)
    {
        SerializedProperty element = spawnableEnemiesProperty.GetArrayElementAtIndex(index);
        SerializedProperty prefabProperty = element.FindPropertyRelative("prefab");
        SerializedProperty percentageProperty = element.FindPropertyRelative("percentage");

        float lineHeight = EditorGUIUtility.singleLineHeight;
        rect.height = lineHeight;

        float prefabWidth = rect.width * 0.6f;
        Rect prefabRect = new Rect(rect.x, rect.y, prefabWidth - 4f, lineHeight);
        Rect percentageRect = new Rect(prefabRect.xMax + 4f, rect.y, rect.width - prefabWidth, lineHeight);

        EditorGUI.PropertyField(prefabRect, prefabProperty, GUIContent.none);
        EditorGUI.Slider(percentageRect, percentageProperty, 0f, 100f);
    }

    private void DrawSpawnPercentageSummary()
    {
        float total = 0f;
        for (int i = 0; i < spawnableEnemiesProperty.arraySize; i++)
        {
            SerializedProperty element = spawnableEnemiesProperty.GetArrayElementAtIndex(i);
            SerializedProperty percentageProperty = element.FindPropertyRelative("percentage");
            total += Mathf.Max(0f, percentageProperty.floatValue);
        }

        EditorGUILayout.HelpBox($"Total configured percentage: {total:0.##}%", MessageType.None);

        if (Mathf.Approximately(total, 100f))
        {
            return;
        }

        MessageType messageType = total <= 0f ? MessageType.Error : MessageType.Warning;
        EditorGUILayout.HelpBox("Percentages do not sum to 100%. Values will be normalized at runtime.", messageType);
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (spawnAreaModeProperty == null || spawnAreaModeProperty.enumValueIndex != 1)
        {
            return;
        }

        EnemySpawner spawner = (EnemySpawner)target;
        if (spawner == null)
        {
            return;
        }

        if (System.Array.IndexOf(Selection.gameObjects, spawner.gameObject) < 0)
        {
            return;
        }

        serializedObject.Update();
        bool changed = DrawControlPointHandles(spawner.transform);

        if (changed)
        {
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    private bool DrawControlPointHandles(Transform ownerTransform)
    {
        if (controlPointsProperty == null || ownerTransform == null)
        {
            return false;
        }

        int count = controlPointsProperty.arraySize;
        if (count == 0)
        {
            return false;
        }

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
        for (int i = 0; i < polygon.Length; i++)
        {
            SerializedProperty element = controlPointsProperty.GetArrayElementAtIndex(i);
            Vector3 worldPoint = polygon[i];
            float handleSize = HandleUtility.GetHandleSize(worldPoint) * 0.075f;

            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.FreeMoveHandle(worldPoint, Quaternion.identity, handleSize, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Move Spawn Area Control Point");
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
