using BehaviourTree;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTreeEditor
{
	public class BehaviourTreeEditor : EditorWindow
	{
		BehaviourTreeGraphView treeView;
		InspectorView inspectorView;

		[MenuItem("DeckBuilder/BehaviourTreeEditor")]
		public static void OpenWindow()
		{
			BehaviourTreeEditor wnd = GetWindow<BehaviourTreeEditor>();
			wnd.titleContent = new GUIContent("Behaviour Tree Editor");
		}

		[OnOpenAsset]
		public static bool OnOpenAsset(int instanceId, int line)
		{
			if (Selection.activeObject is BehaviourTree.BehaviourTree)
			{
				OpenWindow();
				return true;
			}
			return false;
		}

		private void OnInspectorUpdate()
		{
			treeView?.UpdateNodeStates();
		}

		public void CreateGUI()
		{
			VisualElement root = rootVisualElement;

			VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/BehaviourTreeEditor/BehaviourTreeEditor.uxml");
			visualTree.CloneTree(root);

			StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/BehaviourTreeEditor/BehaviourTreeEditor.uss");
			root.styleSheets.Add(styleSheet);

			treeView = root.Q<BehaviourTreeGraphView>();
			inspectorView = root.Q<InspectorView>();

			treeView.OnNodeSelected += OnNodeSelectionChanged;

			OnSelectionChange();
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private void OnDisable()
		{

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void OnPlayModeStateChanged(PlayModeStateChange change)
		{
			switch (change)
			{
				case PlayModeStateChange.EnteredEditMode:
					OnSelectionChange();
					break;
				case PlayModeStateChange.ExitingEditMode:
					break;
				case PlayModeStateChange.EnteredPlayMode:
					OnSelectionChange();
					break;
				case PlayModeStateChange.ExitingPlayMode:
					break;
			}
		}

		private void OnSelectionChange()
		{
			BehaviourTree.BehaviourTree tree = Selection.activeObject as BehaviourTree.BehaviourTree;

			if (!tree && Selection.activeGameObject)
			{
				BehaviourTreeRunner runner = Selection.activeGameObject.GetComponent<BehaviourTreeRunner>();
				tree = runner?.Tree;
			}

			if (Application.isPlaying)
			{
				if (tree)
				{
					treeView?.PopulateView(tree);
				}
			}
			else
			{
				if (tree && tree != null && AssetDatabase.CanOpenAssetInEditor(tree.GetInstanceID()))
				{
					treeView.PopulateView(tree);
				}
			}
		}

		private void OnNodeSelectionChanged(NodeView node)
		{
			inspectorView.UpdateSelection(node);
		}
	}
}
