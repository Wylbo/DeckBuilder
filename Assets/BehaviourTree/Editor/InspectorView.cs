using UnityEngine.UIElements;

namespace BehaviourTree.Editor
{
	[UxmlElement("InspectorView")]
	public partial class InspectorView : VisualElement
	{

		UnityEditor.Editor editor;
		public InspectorView()
		{

		}

		public void UpdateSelection(NodeView node)
		{
			Clear();

			UnityEngine.Object.DestroyImmediate(editor);
			editor = UnityEditor.Editor.CreateEditor(node.node);

			IMGUIContainer container = new IMGUIContainer(() =>
			{
				if (editor.target)
					editor.OnInspectorGUI();
			});
			Add(container);
		}
	}
}
