using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;

[UxmlElement("GraphView")]
public partial class BehaviourTreeGraphView : GraphView
{
    private BehaviourTree tree;
    public event Action<NodeView> OnNodeSelected;

    public BehaviourTreeGraphView()
    {
        Insert(0, new GridBackground());

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/BehaviourTreeEditor.uss");
        styleSheets.Add(styleSheet);

        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        PopulateView(tree);
        AssetDatabase.SaveAssets();
    }

    private NodeView FindNodeView(Node node)
    {
        return GetNodeByGuid(node.guid) as NodeView;
    }

    public void PopulateView(BehaviourTree tree)
    {
        this.tree = tree;

        graphViewChanged -= OnGraphViewChanged;
        DeleteElements(graphElements);
        graphViewChanged += OnGraphViewChanged;

        if (tree.rootNode == null)
        {
            tree.rootNode = tree.CreateNode(typeof(RootNode)) as RootNode;
            EditorUtility.SetDirty(tree);
            AssetDatabase.SaveAssets();
        }

        tree.nodes.ForEach(n => CreateNodeView(n));

        tree.nodes.ForEach(n =>
        {
            var children = tree.GetChildren(n);
            children.ForEach(c =>
            {
                NodeView parent = FindNodeView(n);
                NodeView child = FindNodeView(c);

                Edge edge = parent.output.ConnectTo(child.input);
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

        return graphViewChange;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        TypeCache.TypeCollection actionTypes = TypeCache.GetTypesDerivedFrom<ActionNode>();
        Vector2 localPos = evt.localMousePosition;
        foreach (Type type in actionTypes)
        {
            evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type, localPos));
        }

        TypeCache.TypeCollection compositeTypes = TypeCache.GetTypesDerivedFrom<CompositeNode>();
        foreach (Type type in compositeTypes)
        {
            evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type, localPos));
        }

        TypeCache.TypeCollection DecoratorTypes = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
        foreach (Type type in DecoratorTypes)
        {
            evt.menu.AppendAction($"[{type.BaseType.Name}] {type.Name}", (a) => CreateNode(type, localPos));
        }
    }

    private void CreateNode(Type type, Vector2 position)
    {
        Node node = tree.CreateNode(type);

        // node.position = contentViewContainer.WorldToLocal(position);
        CreateNodeView(node, node.position);
    }

    private void CreateNodeView(Node node, Vector2 position = default)
    {
        NodeView nodeView = new NodeView(node);
        // nodeView.SetPosition(new Rect(contentViewContainer.WorldToLocal(position), Vector2.zero));
        AddElement(nodeView);

        nodeView.OnNodeSelected += OnNodeSelected;

    }
}
