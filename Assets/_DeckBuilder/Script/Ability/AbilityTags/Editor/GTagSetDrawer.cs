#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GTagSet))]
public class GTagSetDrawer : PropertyDrawer
{
    // Per-property state (keyed by propertyPath so arrays/lists work)
    private static readonly Dictionary<string, bool> _expanded = new();
    private static readonly Dictionary<string, int> _popupIdx = new();

    // Layout
    private float Line = EditorGUIUtility.singleLineHeight;
    private const float VPad = 2f;
    private const float BtnW = 22f;   // remove "X"
    private const float Indent = 14f;
    private const float EditBtnW = 64f;  // width for "Tags..." button

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var tagsProp = property.FindPropertyRelative("Tags");
        if (!_expanded.ContainsKey(property.propertyPath)) _expanded[property.propertyPath] = true;
        if (!_popupIdx.ContainsKey(property.propertyPath)) _popupIdx[property.propertyPath] = 0;

        // Header foldout
        var header = new Rect(position.x, position.y, position.width, Line);
        _expanded[property.propertyPath] = EditorGUI.Foldout(header, _expanded[property.propertyPath], label, true);

        if (!_expanded[property.propertyPath])
            return;

        // Content start
        var r = new Rect(position.x + Indent, header.y + Line + VPad, position.width - Indent, Line);

        // Existing tags (read-only)
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            var rowRect = new Rect(r.x, r.y, r.width - BtnW - 4f, Line);
            var remRect = new Rect(r.x + r.width - BtnW, r.y, BtnW, Line);

            var elem = tagsProp.GetArrayElementAtIndex(i);

            using (new EditorGUI.DisabledScope(true))
                EditorGUI.TextField(rowRect, elem.stringValue);

            if (GUI.Button(remRect, "X"))
            {
                tagsProp.DeleteArrayElementAtIndex(i);
                return; // bail to avoid layout issues after deletion
            }

            r.y += Line + VPad;
        }

        // --- Add-from-registry row (popup + "Tags..." button) ---

        // Split row into popup area and "Tags..." button area
        var popupRect = new Rect(r.x, r.y, r.width - EditBtnW - 6f, Line);
        var openRect = new Rect(r.x + r.width - EditBtnW, r.y, EditBtnW, Line);

        // Build options
        var all = (GTagRegistry.All != null)
            ? GTagRegistry.All.OrderBy(s => s, System.StringComparer.Ordinal).ToList()
            : new List<string>();

        var options = new List<string> { "<select a tag>" };
        options.AddRange(all);

        int current = Mathf.Clamp(_popupIdx[property.propertyPath], 0, Mathf.Max(0, options.Count - 1));
        _popupIdx[property.propertyPath] = EditorGUI.Popup(popupRect, "Add from Registry", current, options.ToArray());

        if (_popupIdx[property.propertyPath] > 0)
        {
            string picked = Normalize(options[_popupIdx[property.propertyPath]]);
            if (!Contains(tagsProp, picked))
            {
                tagsProp.arraySize++;
                tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = picked;
            }
            _popupIdx[property.propertyPath] = 0; // reset to placeholder
            GUI.FocusControl(null);
        }

        // Open editor window button
        if (GUI.Button(openRect, "Tags..."))
        {
            // open your editor window (ensure this class exists in an Editor/ folder)
            GTagEditorWindow.Open();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var tagsProp = property.FindPropertyRelative("Tags");
        bool expanded = _expanded.TryGetValue(property.propertyPath, out var ex) ? ex : true;

        float h = Line; // header
        if (expanded)
        {
            int rows = tagsProp.arraySize + 1; // existing tags + popup row
            h += VPad + rows * (Line + VPad);
        }
        return h;
    }

    // Helpers
    private static bool Contains(SerializedProperty listProp, string v)
    {
        for (int i = 0; i < listProp.arraySize; i++)
            if (listProp.GetArrayElementAtIndex(i).stringValue == v) return true;
        return false;
    }

    private static string Normalize(string s) => s?.Trim().ToLowerInvariant() ?? "";
}
#endif
