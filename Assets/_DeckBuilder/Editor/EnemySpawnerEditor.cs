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
    private SerializedProperty spawnAreaDefinitionProperty;
    private SerializedProperty maxAttemptsProperty;
    private SerializedProperty navSampleDistanceProperty;
    private SerializedProperty navAreaMaskProperty;

    private ReorderableList spawnableEnemiesList;

    private void OnEnable()
    {
        enemyManagerProperty = serializedObject.FindProperty("enemyManager");
        spawnableEnemiesProperty = serializedObject.FindProperty("spawnableEnemies");
        spawnOnStartProperty = serializedObject.FindProperty("spawnOnStart");
        spawnCountOnStartProperty = serializedObject.FindProperty("spawnCountOnStart");
        spawnAreaModeProperty = serializedObject.FindProperty("spawnAreaMode");
        radiusProperty = serializedObject.FindProperty("radius");
        spawnAreaDefinitionProperty = serializedObject.FindProperty("spawnAreaDefinition");
        maxAttemptsProperty = serializedObject.FindProperty("maxAttemptsPerSpawn");
        navSampleDistanceProperty = serializedObject.FindProperty("navMeshSampleDistance");
        navAreaMaskProperty = serializedObject.FindProperty("navMeshAreaMask");

        spawnableEnemiesList = new ReorderableList(serializedObject, spawnableEnemiesProperty, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Spawnable Enemies"),
            elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
        };

        spawnableEnemiesList.drawElementCallback = DrawSpawnableEnemyElement;
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
            EditorGUILayout.PropertyField(spawnAreaDefinitionProperty);
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
}
#endif
