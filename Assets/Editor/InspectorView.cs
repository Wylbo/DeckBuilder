using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

[UxmlElement("InspectorView")]
public partial class InspectorView : VisualElement
{

    Editor editor;
    public InspectorView()
    {

    }

    public void UpdateSelection(NodeView node)
    {
        Clear();

        UnityEngine.Object.DestroyImmediate(editor);
        editor = Editor.CreateEditor(node.node);

        IMGUIContainer container = new IMGUIContainer(() =>
        {
            if (editor.target)
                editor.OnInspectorGUI();
        });
        Add(container);
    }
}
