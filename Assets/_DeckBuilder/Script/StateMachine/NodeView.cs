using System;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor;
using UnityEngine.UIElements;
public class NodeView : UnityEditor.Experimental.GraphView.Node
{
    public Node node;
    public Port input;
    public Port output;

    public event Action<NodeView> OnNodeSelected;
    public NodeView(Node node) : base("Assets/Editor/NodeView.uxml")
    {
        this.node = node;
        title = node.name;

        viewDataKey = node.guid;

        style.left = node.position.x;
        style.top = node.position.y;

        CreateInputPorts();
        CreateOutputPorts();
        SetupClasses();
    }

    private void SetupClasses()
    {
        if (node is ActionNode)
        {
            AddToClassList("action");
        }
        else if (node is CompositeNode)
        {
            AddToClassList("composite");
        }
        else if (node is DecoratorNode)
        {
            AddToClassList("decorator");
        }
        else if (node is RootNode)
        {
            AddToClassList("root");
        }
    }

    private void CreateInputPorts()
    {
        if (node is RootNode)
            return;

        input = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));

        if (input != null)
        {
            input.portName = "";
            input.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(input);
        }
    }

    private void CreateOutputPorts()
    {
        if (node is CompositeNode)
            output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
        else if (node is DecoratorNode || node is RootNode)
            output = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));

        if (output != null)
        {
            output.portName = "";
            output.style.flexDirection = FlexDirection.ColumnReverse;
            outputContainer.Add(output);
        }
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);

        Undo.RecordObject(node, "Move Node");

        node.position.x = newPos.xMin;
        node.position.y = newPos.yMin;

        EditorUtility.SetDirty(node);
    }

    public override void OnSelected()
    {
        base.OnSelected();

        OnNodeSelected?.Invoke(this);
    }
}
