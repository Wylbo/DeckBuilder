using System;
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
	public class NodeView : UnityEditor.Experimental.GraphView.Node
	{
		public Node node;
		public Port input;
		public Port output;

		public event Action<NodeView> OnNodeSelected;
		public NodeView(Node node) : base("Assets/BehaviourTree/Editor/NodeView.uxml")
		{
			this.node = node;
			title = node.name;

			viewDataKey = node.GUID;

			style.left = node.Position.x;
			style.top = node.Position.y;

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

			node.SetPosition(newPos);

			EditorUtility.SetDirty(node);
		}

		public override void OnSelected()
		{
			base.OnSelected();

			OnNodeSelected?.Invoke(this);
		}

		public void SortChildren()
		{
			CompositeNode compositeNode = node as CompositeNode;

			if (compositeNode)
			{
				compositeNode.children.Sort(SortByHorizontalPosition);
			}
		}

		private int SortByHorizontalPosition(Node left, Node right)
		{
			return left.Position.x < right.Position.x ? -1 : 1;
		}

		public void UpdateState()
		{
			RemoveFromClassList("running");
			RemoveFromClassList("failure");
			RemoveFromClassList("success");

			if (!Application.isPlaying)
				return;

			switch (node.CurrentState)
			{
				case Node.State.Running:
					if (node.Started)
						AddToClassList("running");
					break;
				case Node.State.Failure:
					AddToClassList("failure");
					break;
				case Node.State.Success:
					AddToClassList("success");
					break;
			}
		}
	}
}