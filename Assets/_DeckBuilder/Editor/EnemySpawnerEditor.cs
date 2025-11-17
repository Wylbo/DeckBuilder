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
    private SerializedProperty maxAttemptsProperty;
    private SerializedProperty navSampleDistanceProperty;
    private SerializedProperty navAreaMaskProperty;

    private ReorderableList spawnableEnemiesList;

    // NavMesh area shown as plain int per request (no reflection/mask UI)

    private void OnEnable()
    {
        enemyManagerProperty = serializedObject.FindProperty("enemyManager");
        spawnableEnemiesProperty = serializedObject.FindProperty("spawnableEnemies");
        spawnOnStartProperty = serializedObject.FindProperty("spawnOnStart");
        spawnCountOnStartProperty = serializedObject.FindProperty("spawnCountOnStart");
        spawnAreaModeProperty = serializedObject.FindProperty("spawnAreaMode");
        radiusProperty = serializedObject.FindProperty("radius");
        controlPointAreaProperty = serializedObject.FindProperty("controlPointArea");
        maxAttemptsProperty = serializedObject.FindProperty("maxAttemptsPerSpawn");
        navSampleDistanceProperty = serializedObject.FindProperty("navMeshSampleDistance");
        navAreaMaskProperty = serializedObject.FindProperty("navMeshAreaMask");

        spawnableEnemiesList = new ReorderableList(serializedObject, spawnableEnemiesProperty, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawnable Enemies"),
            elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
        };

        spawnableEnemiesList.drawElementCallback = DrawSpawnableEnemyElement;

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Core setup
        EditorGUILayout.PropertyField(enemyManagerProperty);
        spawnableEnemiesList.DoLayoutList();
        DrawSpawnPercentageSummary();
        EditorGUILayout.PropertyField(spawnOnStartProperty);
        EditorGUILayout.PropertyField(spawnCountOnStartProperty);

        EditorGUILayout.Space();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(spawnAreaModeProperty);
        bool areaModeChanged = EditorGUI.EndChangeCheck();
        if (spawnAreaModeProperty.enumValueIndex == 0)
        {
            EditorGUILayout.PropertyField(radiusProperty);
        }
        else
        {
            // Hide point list from inspector; edit via Scene view
            EditorGUILayout.HelpBox("Edit control points in the Scene view. Use the on-screen + / - buttons to add or remove points, select a point to move it with the axis gizmos.", MessageType.Info);
            if (areaModeChanged)
            {
                // Ensure we have a valid shape when switching to Control Points
                EnsureMinimumControlPoints(((EnemySpawner)target).transform);
            }
        }

        EditorGUILayout.Space();
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

        float prefabWidth = rect.width * 0.5f;
        float percentFieldWidth = 20f;
        float spacing = 4f;

        Rect prefabRect = new Rect(rect.x, rect.y, prefabWidth - spacing, lineHeight);
        Rect sliderRect = new Rect(prefabRect.xMax + spacing, rect.y, rect.width - prefabWidth - percentFieldWidth - spacing * 2f, lineHeight);
        Rect valueRect = new Rect(sliderRect.xMax + spacing, rect.y, percentFieldWidth, lineHeight);

        EditorGUI.PropertyField(prefabRect, prefabProperty, GUIContent.none);

        EditorGUI.BeginChangeCheck();
        int ival = percentageProperty.intValue;
        ival = Mathf.Clamp(EditorGUI.IntSlider(sliderRect, GUIContent.none, ival, 0, 100), 0, 100);
        if (EditorGUI.EndChangeCheck())
        {
            percentageProperty.intValue = ival;
        }

        // Only show the percent label (read-only), not an editable number field
        EditorGUI.LabelField(valueRect, $"{percentageProperty.intValue}%", EditorStyles.miniLabel);
    }

    private void DrawSpawnPercentageSummary()
    {
        int total = 0;
        for (int i = 0; i < spawnableEnemiesProperty.arraySize; i++)
        {
            SerializedProperty element = spawnableEnemiesProperty.GetArrayElementAtIndex(i);
            SerializedProperty percentageProperty = element.FindPropertyRelative("percentage");
            total += Mathf.Max(0, percentageProperty.intValue);
        }

        EditorGUILayout.HelpBox($"Total configured percentage: {total}%", MessageType.None);

        if (total == 100)
        {
            return;
        }

        MessageType messageType = total <= 0 ? MessageType.Error : MessageType.Warning;
        EditorGUILayout.HelpBox("Percentages do not sum to 100%. Values will be normalized at runtime.", messageType);
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
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

        // Radius mode
        if (spawnAreaModeProperty != null && spawnAreaModeProperty.enumValueIndex == 0)
        {
            DrawRadiusSceneGUI(spawner);
        }
        // Control points mode handled by AreaSelection Scene Tool now
        else if (spawnAreaModeProperty != null && spawnAreaModeProperty.enumValueIndex == 1)
        {
            // Intentionally left empty to avoid duplicate scene gizmos
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRadiusSceneGUI(EnemySpawner spawner)
    {
        Transform t = spawner.transform;
        Handles.color = new Color(0f, 0.85f, 0.3f, 0.35f);
        EditorGUI.BeginChangeCheck();
        float current = radiusProperty.floatValue;
        // Draw an interactive radius handle around the spawner position (XZ plane)
        float newRadius = Handles.RadiusHandle(Quaternion.identity, t.position, current);
        if (EditorGUI.EndChangeCheck())
        {
            radiusProperty.floatValue = Mathf.Max(0.1f, newRadius);
            SceneView.RepaintAll();
        }

        // Outline to match visual style
        Handles.color = Color.green;
        Handles.DrawWireDisc(t.position, Vector3.up, radiusProperty.floatValue);
    }

    private void EnsureMinimumControlPoints(Transform t)
    {
        if (t == null) return;
        SerializedProperty pointsProp = controlPointAreaProperty.FindPropertyRelative("controlPoints");
        if (pointsProp == null) return;
        if (pointsProp.arraySize >= 3) return;

        // Clear existing points
        for (int i = pointsProp.arraySize - 1; i >= 0; i--)
        {
            pointsProp.DeleteArrayElementAtIndex(i);
        }

        // Build an equilateral triangle centered at origin using current radius for scale
        float r = Mathf.Max(1f, radiusProperty.floatValue);
        float h = Mathf.Sqrt(3f) * 0.5f * r; // height of equilateral triangle with side ~ r
        Vector3 p0 = new Vector3(0f, 0f, r * 0.5f);
        Vector3 p1 = new Vector3(-h, 0f, -r * 0.5f);
        Vector3 p2 = new Vector3(h, 0f, -r * 0.5f);

        pointsProp.InsertArrayElementAtIndex(0);
        pointsProp.GetArrayElementAtIndex(0).vector3Value = p0;
        pointsProp.InsertArrayElementAtIndex(1);
        pointsProp.GetArrayElementAtIndex(1).vector3Value = p1;
        pointsProp.InsertArrayElementAtIndex(2);
        pointsProp.GetArrayElementAtIndex(2).vector3Value = p2;

        Undo.RecordObject(target, "Create Default Control Points");
        serializedObject.ApplyModifiedProperties();
        SceneView.RepaintAll();
    }



}
#endif
