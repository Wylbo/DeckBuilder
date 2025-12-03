#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AreaSelection))]
public class AreaSelectionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var r = position;
        r.height = EditorGUIUtility.singleLineHeight;
        EditorGUI.LabelField(r, label);
        r.y += r.height + 2f;
        EditorGUI.HelpBox(r, "Edit in Scene view: Shift+Click to insert on edges; use overlay to add/remove, center, and switch axis.", MessageType.None);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2f + 6f;
    }
}
#endif
