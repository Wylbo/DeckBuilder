#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AbilityStatEntry))]
public class AbilityStatEntryDrawer : PropertyDrawer
{
    private const float VPad = 2f;
    private const float Spacing = 6f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null)
            return;

        EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.IndentedRect(position);

        var keyProp = property.FindPropertyRelative("Key");
        var sourceProp = property.FindPropertyRelative("Source");
        var valueProp = property.FindPropertyRelative("Value");
        var globalProp = property.FindPropertyRelative("GlobalKey");

        // First line: Key + Source
        var line = new Rect(position.x, position.y + VPad, position.width, EditorGUIUtility.singleLineHeight);
        float keyWidth = Mathf.Floor(line.width * 0.45f);
        var keyRect = new Rect(line.x, line.y, keyWidth, line.height);
        var sourceRect = new Rect(keyRect.xMax + Spacing, line.y, line.width - keyWidth - Spacing, line.height);

        EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
        EditorGUI.PropertyField(sourceRect, sourceProp, GUIContent.none);

        // Second line depends on source
        var second = new Rect(position.x, line.yMax + VPad, position.width, EditorGUIUtility.singleLineHeight);
        var source = (AbilityStatSource)sourceProp.enumValueIndex;

        switch (source)
        {
            case AbilityStatSource.Flat:
                EditorGUI.PropertyField(second, valueProp, new GUIContent("Value"));
                break;
            case AbilityStatSource.RatioToGlobal:
                DrawGlobalWithValue(second, globalProp, valueProp, "Ratio", 0.5f);
                break;
            case AbilityStatSource.CopyGlobal:
                DrawGlobalWithHint(second, globalProp, "Uses global value");
                break;
        }

        EditorGUI.EndProperty();
    }

    private static void DrawGlobalWithValue(Rect rect, SerializedProperty globalProp, SerializedProperty valueProp, string valueLabel, float globalWidthRatio)
    {
        float globalWidth = Mathf.Floor(rect.width * globalWidthRatio);
        var globalRect = new Rect(rect.x, rect.y, globalWidth - Spacing, rect.height);
        var valueRect = new Rect(globalRect.xMax + Spacing, rect.y, rect.width - globalWidth, rect.height);

        EditorGUI.PropertyField(globalRect, globalProp, GUIContent.none);
        EditorGUI.PropertyField(valueRect, valueProp, new GUIContent(valueLabel));
    }

    private static void DrawGlobalWithHint(Rect rect, SerializedProperty globalProp, string hint)
    {
        float globalWidth = Mathf.Floor(rect.width * 0.6f);
        var globalRect = new Rect(rect.x, rect.y, globalWidth - Spacing, rect.height);
        var hintRect = new Rect(globalRect.xMax + Spacing, rect.y, rect.width - globalWidth, rect.height);

        EditorGUI.PropertyField(globalRect, globalProp, GUIContent.none);
        EditorGUI.LabelField(hintRect, hint, EditorStyles.miniLabel);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property == null)
            return EditorGUIUtility.singleLineHeight;

        var sourceProp = property.FindPropertyRelative("Source");
        var source = sourceProp != null ? (AbilityStatSource)sourceProp.enumValueIndex : AbilityStatSource.Flat;
        // Two lines + padding
        float line = EditorGUIUtility.singleLineHeight + VPad;
        switch (source)
        {
            case AbilityStatSource.Flat:
            case AbilityStatSource.RatioToGlobal:
            case AbilityStatSource.CopyGlobal:
                return line * 2f + VPad;
            default:
                return line * 2f + VPad;
        }
    }
}
#endif
