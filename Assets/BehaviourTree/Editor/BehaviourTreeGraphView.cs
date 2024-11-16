using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTree.Nodes;
using BehaviourTree.Nodes.ActionNode;
using BehaviourTree.Nodes.CompositeNode;
using BehaviourTree.Nodes.DecoratorNode;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Node = BehaviourTree.Nodes.Node;

namespace BehaviourTree.Editor
{
	[UxmlElement("GraphView")]
	public partial class BehaviourTreeGraphView : GraphView
	{
		private BehaviourTree tree;
		public event Action<NodeView> OnNodeSelected;

		public EditorWindow EditorWindow { get; set; }
		public BTSearchWindow searchWindow;

		public BehaviourTreeGraphView()
		{
			Insert(0, new GridBackground());

			this.AddManipulator(new ContentZoomer());
			this.AddManipulator(new ContentDragger());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new RectangleSelector());

			StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/BehaviourTree/Editor/BehaviourTreeEditor.uss");
			styleSheets.Add(styleSheet);

			Undo.undoRedoPerformed += OnUndoRedo;

			AddSearchWindow();
		}

		private void AddSearchWindow()
		{
			if (searchWindow == null)
			{
				searchWindow = ScriptableObject.CreateInstance<BTSearchWindow>();
				searchWindow.Initialize(this, EditorWindow);
			}

			nodeCreationRequest = context =>
			{
				Vector2 position = context.screenMousePosition;
				SearchWindow.Open(new SearchWindowContext(position), searchWindow);
			};
		}

		private void OnUndoRedo()
		{
			PopulateView(tree);
			AssetDatabase.SaveAssets();
		}

		private NodeView FindNodeView(Node node)
		{
			return GetNodeByGuid(node.GUID) as NodeView;
		}

		public void PopulateView(BehaviourTree tree)
		{
			this.tree = tree;

			graphViewChanged -= OnGraphViewChanged;
			DeleteElements(graphElements);
			graphViewChanged += OnGraphViewChanged;

			if (tree?.rootNode == null)
			{
				tree.rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
				EditorUtility.SetDirty(tree);
				AssetDatabase.SaveAssets();
			}

			tree?.nodes.ForEach(n => CreateNodeView(n));

			tree?.nodes.ForEach(n =>
			{
				var children = tree.GetChildren(n);
				children.ForEach(c =>
				{
					NodeView parent = FindNodeView(n);
					NodeView child = FindNodeView(c);

					Edge edge = parent.output.ConnectTo(child?.input);
					AddElement(edge);
				});
			});
		}

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
		{
			return ports.ToList().Where(endPort =>
				endPort.direction != startPort.direction &&
				endPort.node != startPort.node).ToList();
		}

		private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
		{
			graphViewChange.elementsToRemove?.ForEach(elem =>
			{
				if (elem is NodeView nodeView)
				{
					nodeView.OnNodeSelected -= OnNodeSelected;
					tree.DeleteNode(nodeView.node);
				}

				if (elem is Edge edge)
				{
					NodeView parentView = edge.output.node as NodeView;
					NodeView childView = edge.input.node as NodeView;
					tree.RemoveChild(parentView.node, childView.node);
				}
			});

			graphViewChange.edgesToCreate?.ForEach(edge =>
			{
				NodeView parentView = edge.output.node as NodeView;
				NodeView childView = edge.input.node as NodeView;
				tree.AddChild(parentView.node, childView.node);
			});

			if (graphViewChange.movedElements != null)
			{
				nodes.ForEach((n) =>
				{
					NodeView view = n as NodeView;
					view.SortChildren();
				});
			}

			return graphViewChange;
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			// base.BuildContextualMenu(evt);
			Vector2 localPos = evt.mousePosition;

			localPos = contentViewContainer.WorldToLocal(localPos);

			CreateContextualMenuGroup(evt, localPos);
			CreateContextualMenuNode(evt, localPos);
		}

		private void CreateContextualMenuNode(ContextualMenuPopulateEvent evt, Vector2 localPos)
		{
			TypeCache.TypeCollection actionTypes = TypeCache.GetTypesDerivedFrom<ActionNode>();
			foreach (Type type in actionTypes)
			{
				evt.menu.AppendAction($"[{type.BaseType.Name}]/{type.Name}", (a) => CreateNode(type, localPos));
			}

			TypeCache.TypeCollection compositeTypes = TypeCache.GetTypesDerivedFrom<CompositeNode>();
			foreach (Type type in compositeTypes)
			{
				evt.menu.AppendAction($"[{type.BaseType.Name}]/{type.Name}", (a) => CreateNode(type, localPos));
			}

			TypeCache.TypeCollection DecoratorTypes = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
			foreach (Type type in DecoratorTypes)
			{
				evt.menu.AppendAction($"[{type.BaseType.Name}]/{type.Name}", (a) => CreateNode(type, localPos));
			}
		}

		private void CreateContextualMenuGroup(ContextualMenuPopulateEvent evt, Vector2 localPos)
		{
			evt.menu.AppendAction("Add Group", (a) => CreateGroup("Group", localPos));
		}

		private void CreateGroup(string title, Vector2 localPos)
		{
			Group group = new Group()
			{
				title = title
			};

			group.SetPosition(new Rect(localPos, Vector2.zero));

			AddElement(group);
		}

		public void CreateNode(Type type, Vector2 position)
		{
			Node node = tree.CreateNode(type);
			node.Position = position;
			CreateNodeView(node);
		}

		private void CreateNodeView(Node node)
		{
			NodeView nodeView = new NodeView(node);
			AddElement(nodeView);

			nodeView.OnNodeSelected += OnNodeSelected;
		}

		public void UpdateNodeStates()
		{
			nodes.ForEach((n) =>
			{
				NodeView view = n as NodeView;
				view.UpdateState();
			});
		}
	}
}
