#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AbilityStatOp))]
public class AbilityStatOpDrawer : PropertyDrawer
{
    private const float Spacing = 6f;
    private const float VPad = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property == null)
            return;

        EditorGUI.BeginProperty(position, label, property);

        // Single-line layout with label prefix
        position.height = EditorGUIUtility.singleLineHeight;
        position = EditorGUI.IndentedRect(position);
        var contentRect = EditorGUI.PrefixLabel(position, label);

        // Find sub-properties
        var keyProp = property.FindPropertyRelative("Key");
        var opProp = property.FindPropertyRelative("OpType");
        var valProp = property.FindPropertyRelative("Value");

        // Column widths: Key 40%, OpType 30%, Value 30%
        float total = contentRect.width;
        float keyW = Mathf.Floor(total * 0.4f);
        float opW = Mathf.Floor(total * 0.3f);
        float valW = total - keyW - opW - 2 * Spacing;

        var keyRect = new Rect(contentRect.x, contentRect.y + VPad, keyW, EditorGUIUtility.singleLineHeight);
        var opRect  = new Rect(keyRect.xMax + Spacing, contentRect.y + VPad, opW, EditorGUIUtility.singleLineHeight);
        var valRect = new Rect(opRect.xMax + Spacing, contentRect.y + VPad, valW, EditorGUIUtility.singleLineHeight);

        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
        EditorGUI.PropertyField(opRect, opProp, GUIContent.none);
        EditorGUI.PropertyField(valRect, valProp, GUIContent.none);
        EditorGUI.indentLevel = oldIndent;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight + 2f * VPad;
    }
}
#endif

