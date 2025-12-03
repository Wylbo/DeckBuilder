#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(AbilityModifier), true)]
public class AbilityModifierEditor : Editor
{
    #region Fields
    private SerializedProperty queryProp;
    private SerializedProperty operationsProp;
    private SerializedProperty orderProp;
    private SerializedProperty stackGroupProp;
    private SerializedProperty maxStacksProp;
    private ReorderableList operationsList;
    private static bool stackingFoldout;
    private ReorderableList queryAllList;
    private ReorderableList queryAnyList;
    private ReorderableList queryNoneList;
    #endregion

    #region Initialization
    private void OnEnable()
    {
        queryProp = serializedObject.FindProperty("query");
        operationsProp = serializedObject.FindProperty("operations");
        orderProp = serializedObject.FindProperty("order");
        stackGroupProp = serializedObject.FindProperty("stackGroup");
        maxStacksProp = serializedObject.FindProperty("maxStacks");

        if (operationsProp != null)
        {
            operationsList = new ReorderableList(serializedObject, operationsProp, true, true, true, true);

            operationsList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Operations");
            };

            operationsList.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= operationsProp.arraySize) return;
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, false);

                rect.y += 2f;
                var element = operationsProp.GetArrayElementAtIndex(index);
                float height = EditorGUI.GetPropertyHeight(element, GUIContent.none, true);
                rect.height = height;
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };

            operationsList.elementHeightCallback = idx =>
            {
                if (idx < 0 || idx >= operationsProp.arraySize) return EditorGUIUtility.singleLineHeight + 4f;
                var element = operationsProp.GetArrayElementAtIndex(idx);
                return EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + 4f;
            };

            operationsList.onAddCallback = l =>
            {
                operationsProp.arraySize++;
                serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
            };

            operationsList.onRemoveCallback = l =>
            {
                int idx = Mathf.Clamp(l.index, 0, operationsProp.arraySize - 1);
                if (operationsProp.arraySize > 0)
                {
                    operationsProp.DeleteArrayElementAtIndex(idx);
                    serializedObject.ApplyModifiedProperties();
                    l.index = Mathf.Clamp(idx - 1, -1, operationsProp.arraySize - 1);
                    GUI.changed = true;
                }
            };

            operationsList.onReorderCallback = l =>
            {
                serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
            };
        }
    }
    #endregion

    #region Inspector GUI
    public override void OnInspectorGUI()
    {
        if (serializedObject == null) return;
        serializedObject.Update();

        DrawQuerySection();

        EditorGUILayout.Space();

        DrawOperationsSection();

        EditorGUILayout.Space();

        DrawStackingSection();

        serializedObject.ApplyModifiedProperties();
    }
    #endregion

    #region Render Helpers
    private void DrawQuerySection()
    {
        if (queryProp == null) return;

        EditorGUILayout.LabelField("Query", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope())
        {
            var allProp = queryProp.FindPropertyRelative("All");
            var anyProp = queryProp.FindPropertyRelative("Any");
            var noneProp = queryProp.FindPropertyRelative("None");

            if (queryAllList == null && allProp != null)
                queryAllList = CreateTagSetList(serializedObject, allProp, "All");
            if (queryAnyList == null && anyProp != null)
                queryAnyList = CreateTagSetList(serializedObject, anyProp, "Any");
            if (queryNoneList == null && noneProp != null)
                queryNoneList = CreateTagSetList(serializedObject, noneProp, "None");

            if (queryAllList != null) queryAllList.DoLayoutList();
            if (queryAnyList != null) queryAnyList.DoLayoutList();
            if (queryNoneList != null) queryNoneList.DoLayoutList();
        }
    }

    private void DrawOperationsSection()
    {
        if (operationsList != null)
            operationsList.DoLayoutList();
    }

    private void DrawStackingSection()
    {
        stackingFoldout = EditorGUILayout.Foldout(stackingFoldout, "Stacking & Order", true);
        if (!stackingFoldout) return;

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            if (orderProp != null) EditorGUILayout.PropertyField(orderProp);
            if (stackGroupProp != null) EditorGUILayout.PropertyField(stackGroupProp);
            if (maxStacksProp != null) EditorGUILayout.PropertyField(maxStacksProp);
        }
    }

    private ReorderableList CreateTagSetList(SerializedObject so, SerializedProperty tagSetProp, string header)
    {
        var tagsProp = tagSetProp.FindPropertyRelative("Tags");
        if (tagsProp == null || !tagsProp.isArray) return null;

        var list = new ReorderableList(so, tagsProp, true, true, true, true)
        {
            draggable = true
        };

        list.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, header); };
        list.drawElementCallback = (rect, index, active, focused) =>
        {
            if (index < 0 || index >= tagsProp.arraySize) return;
            ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, active, focused, false);
            rect.y += 2f; rect.height = EditorGUIUtility.singleLineHeight;
            var elem = tagsProp.GetArrayElementAtIndex(index);
            EditorGUI.LabelField(rect, elem.stringValue, EditorStyles.textField);
        };
        list.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f;
        list.onAddDropdownCallback = (buttonRect, l) =>
        {
            var menu = new GenericMenu();
            var all = (GTagRegistry.All != null)
                ? GTagRegistry.All.OrderBy(s => s, System.StringComparer.Ordinal).ToList()
                : new System.Collections.Generic.List<string>();
            foreach (var t in all)
            {
                var tag = Normalize(t);
                menu.AddItem(new GUIContent(tag), false, () =>
                {
                    if (!Contains(tagsProp, tag))
                    {
                        tagsProp.arraySize++;
                        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                        so.ApplyModifiedProperties();
                        GUI.changed = true;
                    }
                });
            }
            menu.ShowAsContext();
        };
        list.onCanRemoveCallback = l => l.index >= 0 && l.index < tagsProp.arraySize;
        list.onRemoveCallback = l =>
        {
            int idx = Mathf.Clamp(l.index, 0, tagsProp.arraySize - 1);
            if (tagsProp.arraySize > 0)
            {
                tagsProp.DeleteArrayElementAtIndex(idx);
                so.ApplyModifiedProperties();
                l.index = Mathf.Clamp(idx - 1, -1, tagsProp.arraySize - 1);
                GUI.changed = true;
            }
        };
        list.onReorderCallback = l =>
        {
            so.ApplyModifiedProperties();
            GUI.changed = true;
        };

        return list;
    }

    private static bool Contains(SerializedProperty listProp, string v)
    {
        for (int i = 0; i < listProp.arraySize; i++)
            if (listProp.GetArrayElementAtIndex(i).stringValue == v) return true;
        return false;
    }

    private static string Normalize(string s) => s?.Trim().ToLowerInvariant() ?? "";
    #endregion
}
#endif
