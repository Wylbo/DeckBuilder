using BehaviourTree;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace BehaviourTree.Editor
{
	public class BehaviourTreeEditor : EditorWindow
	{
		private BehaviourTreeGraphView treeView;
		private InspectorView inspectorView;
		private IMGUIContainer blackboardView;

		private SerializedObject treeObject;
		private SerializedProperty blackboardProp;

		[MenuItem("DeckBuilder/BehaviourTreeEditor")]
		public static void OpenWindow()
		{
			BehaviourTreeEditor wnd = GetWindow<BehaviourTreeEditor>();
			wnd.titleContent = new GUIContent("Behaviour Tree Editor");
		}

		[OnOpenAsset]
		public static bool OnOpenAsset(int instanceId, int line)
		{
			if (Selection.activeObject is BehaviourTree)
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

			VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/BehaviourTree/Editor/BehaviourTreeEditor.uxml");
			visualTree.CloneTree(root);

			StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/BehaviourTree/Editor/BehaviourTreeEditor.uss");
			root.styleSheets.Add(styleSheet);

			treeView = root.Q<BehaviourTreeGraphView>();
			treeView.EditorWindow = this;
			treeView.searchWindow.editorWindow = this;
			inspectorView = root.Q<InspectorView>();
			blackboardView = root.Q<IMGUIContainer>();

			OnSelectionChange();

			if (treeObject != null && treeObject.targetObject != null)
			{
				blackboardView.onGUIHandler = () =>
				{
					if (treeObject.targetObject == null)
						return;

					treeObject.Update();
					EditorGUILayout.PropertyField(blackboardProp, true);
					treeObject.ApplyModifiedProperties();
				};
			}

			treeView.OnNodeSelected += OnNodeSelectionChanged;
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
			// get selected BT SO
			BehaviourTree tree = Selection.activeObject as BehaviourTree;

			// or get BT on selected GO
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
				if (tree && AssetDatabase.CanOpenAssetInEditor(tree.GetInstanceID()))
				{
					treeView.PopulateView(tree);
				}
			}

			if (tree != null)
			{
				treeObject = new SerializedObject(tree);
				blackboardProp = treeObject.FindProperty(nameof(tree.blackboard));
				Repaint();
			}
		}

		private void OnNodeSelectionChanged(NodeView node)
		{
			inspectorView.UpdateSelection(node);
		}
	}
}
