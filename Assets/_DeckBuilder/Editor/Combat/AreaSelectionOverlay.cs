#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), id: "AreaSelectionOverlay", displayName: "Area Selection", defaultDisplay: true)]
public class AreaSelectionOverlay : ToolbarOverlay
{
    public AreaSelectionOverlay() : base(
        AreaSelectionShowOnlySelected.ID
        )
    { }

    internal static bool TryGetCurrent(out SerializedObject so, out SerializedProperty areaProp, out Transform owner)
    {
        so = null; areaProp = null; owner = null;
        var active = Selection.activeGameObject;
        if (active == null) return false;
        foreach (var comp in active.GetComponents<Component>())
        {
            if (comp == null) continue;
            var s = new SerializedObject(comp);
            var it = s.GetIterator();
            bool enterChildren = true;
            while (it.Next(enterChildren))
            {
                enterChildren = false;
                if (it.propertyType == SerializedPropertyType.Generic && it.type == nameof(AreaSelection))
                {
                    so = s;
                    areaProp = it.Copy();
                    owner = comp.transform;
                    return true;
                }
            }
        }
        return false;
    }
}

[Overlay(typeof(SceneView), id: "AreaSelectionControlsOverlay", displayName: "Area Selection Controls", defaultDisplay: true)]
public class AreaSelectionControlsOverlay : ToolbarOverlay
{
    public AreaSelectionControlsOverlay() : base(
        AreaSelectionToggleAngles.ID,
        AreaSelectionToggleWalls.ID,
        AreaSelectionToggleTop.ID,
        AreaSelectionAddPoint.ID,
        AreaSelectionRemovePoint.ID,
        AreaSelectionCenter.ID,
        AreaSelectionCenterTransform.ID
    )
    {
        void UpdateDisplayed()
        {
            this.displayed = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _);
        }
        Selection.selectionChanged += UpdateDisplayed;
        UpdateDisplayed();
    }
}
[EditorToolbarElement(AreaSelectionShowOnlySelected.ID, typeof(SceneView))]
public class AreaSelectionShowOnlySelected : EditorToolbarToggle
{
    public const string ID = "AreaSelection/ShowOnlySelected";
    public AreaSelectionShowOnlySelected()
    {
        text = "Show only selected";
        tooltip = "Hide non-selected prisms";
        value = AreaSelectionSceneTool.GetShowOnlySelected();
        this.RegisterValueChangedCallback(evt => { AreaSelectionSceneTool.SetShowOnlySelected(evt.newValue); });
    }
}

[EditorToolbarElement(AreaSelectionAddPoint.ID, typeof(SceneView))]
public class AreaSelectionAddPoint : EditorToolbarButton
{
    public const string ID = "AreaSelection/Add";
    public AreaSelectionAddPoint()
    {
        tooltip = "Add a control point";
        text = "Add";
        icon = EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D;
        style.marginRight = 1; // 1px gap before next button
        // Segmented button styling
        AddToClassList("unity-editor-toolbar__button-strip-element");
        AddToClassList("unity-editor-toolbar__button-strip-element--left");
        UpdateVisibility();
        Selection.selectionChanged += UpdateVisibility;

        clicked += () =>
        {
            if (!AreaSelectionOverlay.TryGetCurrent(out var so, out var areaProp, out var owner)) return;
            var points = areaProp.FindPropertyRelative("controlPoints");
            if (points == null) return;
            Vector3 newWorld = owner.position + owner.right + owner.forward;
            Vector3 local = owner.InverseTransformPoint(newWorld);
            int idx = points.arraySize;
            points.InsertArrayElementAtIndex(idx);
            points.GetArrayElementAtIndex(idx).vector2Value = new Vector2(local.x, local.z);
            AreaSelectionSceneTool.SetSelectedIndexFor(so, areaProp, idx);
            Undo.RecordObject(so.targetObject, "Add Control Point");
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        };
    }

    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

[EditorToolbarElement(AreaSelectionRemovePoint.ID, typeof(SceneView))]
public class AreaSelectionRemovePoint : EditorToolbarButton
{
    public const string ID = "AreaSelection/Remove";
    public AreaSelectionRemovePoint()
    {
        tooltip = "Remove selected control point";
        var tex = EditorGUIUtility.IconContent("Toolbar Minus").image as Texture2D;
        icon = tex;
        text = "Remove";
        style.marginLeft = 0; // keep clustered; gap handled by previous button
        // Segmented button styling
        AddToClassList("unity-editor-toolbar__button-strip-element");
        AddToClassList("unity-editor-toolbar__button-strip-element--right");
        UpdateVisibility();
        Selection.selectionChanged += UpdateVisibility;
        clicked += () =>
        {
            if (!AreaSelectionOverlay.TryGetCurrent(out var so, out var areaProp, out _)) return;
            var points = areaProp.FindPropertyRelative("controlPoints");
            if (points == null || points.arraySize == 0) return;
            int sel = AreaSelectionSceneTool.GetSelectedIndexFor(so, areaProp);
            if (sel < 0 || sel >= points.arraySize) return;
            points.DeleteArrayElementAtIndex(sel);
            int newSel = Mathf.Clamp(sel - 1, -1, points.arraySize - 1);
            AreaSelectionSceneTool.SetSelectedIndexFor(so, areaProp, newSel);
            Undo.RecordObject(so.targetObject, "Remove Control Point");
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        };
    }

    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

[EditorToolbarElement(AreaSelectionCenter.ID, typeof(SceneView))]
public class AreaSelectionCenter : EditorToolbarButton
{
    public const string ID = "AreaSelection/Center";
    public AreaSelectionCenter()
    {
        text = "Center";
        tooltip = "Recenter polygon around centroid";
        // Segmented pair with Center Xform
        AddToClassList("unity-editor-toolbar__button-strip-element");
        AddToClassList("unity-editor-toolbar__button-strip-element--left");
        UpdateVisibility();
        Selection.selectionChanged += UpdateVisibility;
        clicked += () =>
        {
            if (!AreaSelectionOverlay.TryGetCurrent(out var so, out var areaProp, out _)) return;
            var points = areaProp.FindPropertyRelative("controlPoints");
            if (points == null || points.arraySize == 0) return;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.arraySize; i++) sum += points.GetArrayElementAtIndex(i).vector2Value;
            Vector2 centroid = sum / Mathf.Max(1, points.arraySize);
            Undo.RecordObject(so.targetObject, "Center Control Points");
            for (int i = 0; i < points.arraySize; i++)
            {
                var p = points.GetArrayElementAtIndex(i).vector2Value;
                points.GetArrayElementAtIndex(i).vector2Value = p - centroid;
            }
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        };
    }

    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

[EditorToolbarElement(AreaSelectionHeightDropdown.ID, typeof(SceneView))]
public class AreaSelectionHeightDropdown : EditorToolbarDropdown
{
    public const string ID = "AreaSelection/Height";
    public AreaSelectionHeightDropdown()
    {
        text = "Height";
        tooltip = "Set extrusion height";
        clicked += ShowMenu;
        Selection.selectionChanged += UpdateTextFromSelection;
        Selection.selectionChanged += UpdateVisibility;
        UpdateTextFromSelection();
        UpdateVisibility();
    }

    private void UpdateTextFromSelection()
    {
        if (AreaSelectionOverlay.TryGetCurrent(out var so, out var areaProp, out _))
        {
            var hp = areaProp.FindPropertyRelative("height");
            if (hp != null)
            {
                text = $"Height {Mathf.Max(0f, hp.floatValue):0.##}";
                return;
            }
        }
        text = "Height";
    }

    private void ShowMenu()
    {
        if (!AreaSelectionOverlay.TryGetCurrent(out var so, out var areaProp, out _)) return;
        var heightProp = areaProp.FindPropertyRelative("height");
        if (heightProp == null) return;
        float current = Mathf.Max(0f, heightProp.floatValue);
        var menu = new GenericMenu();
        menu.AddDisabledItem(new GUIContent($"Current: {current:0.##}"));
        menu.AddSeparator("");
        void SetVal(float v)
        {
            Undo.RecordObject(so.targetObject, "Change Area Height");
            heightProp.floatValue = Mathf.Max(0f, v);
            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
            UpdateTextFromSelection();
        }
        menu.AddItem(new GUIContent("Set/0"), false, () => SetVal(0f));
        menu.AddItem(new GUIContent("Set/1"), false, () => SetVal(1f));
        menu.AddItem(new GUIContent("Set/2"), false, () => SetVal(2f));
        menu.AddItem(new GUIContent("Set/3"), false, () => SetVal(3f));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Adjust/+0.5"), false, () => SetVal(current + 0.5f));
        menu.AddItem(new GUIContent("Adjust/+1"), false, () => SetVal(current + 1f));
        menu.AddItem(new GUIContent("Adjust/-0.5"), false, () => SetVal(current - 0.5f));
        menu.AddItem(new GUIContent("Adjust/-1"), false, () => SetVal(current - 1f));
        menu.ShowAsContext();
    }

    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}


[EditorToolbarElement(AreaSelectionCenterTransform.ID, typeof(SceneView))]
public class AreaSelectionCenterTransform : EditorToolbarButton
{
    public const string ID = "AreaSelection/CenterTransform";
    public AreaSelectionCenterTransform()
    {
        text = "Center Xform";
        tooltip = "Move transform to polygon centroid (keep shape in place)";
        // Segmented pair with Center
        AddToClassList("unity-editor-toolbar__button-strip-element");
        AddToClassList("unity-editor-toolbar__button-strip-element--right");
        UpdateVisibility();
        Selection.selectionChanged += UpdateVisibility;
        clicked += () =>
        {
            if (!AreaSelectionOverlay.TryGetCurrent(out var so, out var areaProp, out var owner)) return;
            var points = areaProp.FindPropertyRelative("controlPoints");
            if (points == null || points.arraySize == 0 || owner == null) return;

            // Compute local 2D centroid
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < points.arraySize; i++) sum += points.GetArrayElementAtIndex(i).vector2Value;
            Vector2 centroid = sum / Mathf.Max(1, points.arraySize);

            // World position of centroid on the base plane (local y = 0)
            Vector3 worldCentroid = owner.TransformPoint(new Vector3(centroid.x, 0f, centroid.y));

            // Record both transform and points so it is a single undo step
            Undo.RecordObjects(new Object[] { owner.transform, so.targetObject }, "Move Transform To Center");

            // Move transform to centroid and offset points to keep world shape unchanged
            owner.position = worldCentroid;
            for (int i = 0; i < points.arraySize; i++)
            {
                var p = points.GetArrayElementAtIndex(i).vector2Value;
                points.GetArrayElementAtIndex(i).vector2Value = p - centroid;
            }

            so.ApplyModifiedProperties();
            SceneView.RepaintAll();
        };
    }

    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

[EditorToolbarElement(AreaSelectionToggleAngles.ID, typeof(SceneView))]
public class AreaSelectionToggleAngles : EditorToolbarToggle
{
    public const string ID = "AreaSelection/Toggles/Angles";
    public AreaSelectionToggleAngles()
    {
        text = "Angles";
        tooltip = "Enable/disable angle (point) handles";
        value = AreaSelectionSceneTool.GetShowAngles();
        AddToClassList("unity-editor-toolbar__button-strip-element");
        AddToClassList("unity-editor-toolbar__button-strip-element--left");
        this.RegisterValueChangedCallback(evt => { AreaSelectionSceneTool.SetShowAngles(evt.newValue); });
        Selection.selectionChanged += UpdateVisibility;
        UpdateVisibility();
    }
    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

[EditorToolbarElement(AreaSelectionToggleWalls.ID, typeof(SceneView))]
public class AreaSelectionToggleWalls : EditorToolbarToggle
{
    public const string ID = "AreaSelection/Toggles/Walls";
    public AreaSelectionToggleWalls()
    {
        text = "Walls";
        tooltip = "Enable/disable wall (side plane) sliders";
        value = AreaSelectionSceneTool.GetShowWalls();
        AddToClassList("unity-editor-toolbar__button-strip-element");
        AddToClassList("unity-editor-toolbar__button-strip-element--middle");
        this.RegisterValueChangedCallback(evt => { AreaSelectionSceneTool.SetShowWalls(evt.newValue); });
        Selection.selectionChanged += UpdateVisibility;
        UpdateVisibility();
    }
    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}

[EditorToolbarElement(AreaSelectionToggleTop.ID, typeof(SceneView))]
public class AreaSelectionToggleTop : EditorToolbarToggle
{
    public const string ID = "AreaSelection/Toggles/Top";
    public AreaSelectionToggleTop()
    {
        text = "Top";
        tooltip = "Enable/disable top plane (height) handle";
        value = AreaSelectionSceneTool.GetShowTop();
        AddToClassList("unity-editor-toolbar__button-strip-element");
        AddToClassList("unity-editor-toolbar__button-strip-element--right");
        this.RegisterValueChangedCallback(evt => { AreaSelectionSceneTool.SetShowTop(evt.newValue); });
        Selection.selectionChanged += UpdateVisibility;
        UpdateVisibility();
    }
    private void UpdateVisibility()
    {
        style.display = AreaSelectionOverlay.TryGetCurrent(out _, out _, out _) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
#endif
