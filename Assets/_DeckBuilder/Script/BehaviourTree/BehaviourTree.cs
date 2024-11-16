using System;
using System.Collections.Generic;
using BehaviourTree.Node;
using BehaviourTree.Node.CompositeNode;
using BehaviourTree.Node.DecoratorNode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace BehaviourTree
{
	[CreateAssetMenu(fileName = nameof(BehaviourTree), menuName = FileName.BehaviourTree + nameof(BehaviourTree))]
	public class BehaviourTree : ScriptableObject
	{
		[HideInInspector] public Node.Node rootNode;
		[HideInInspector] public Node.Node.State treeState = Node.Node.State.Running;
		[HideInInspector] public List<Node.Node> nodes = new List<Node.Node>();

		[SerializeField, InlineEditor] public Blackboard blackboard = null;

		public BehaviourTree Clone()
		{
			BehaviourTree tree = Instantiate(this);

			tree.blackboard = Instantiate(blackboard);

			tree.rootNode = tree.rootNode.Clone();
			tree.nodes = new List<Node.Node>();

			Traverse(tree.rootNode, (n) =>
			{
				tree.nodes.Add(n);
			});

			return tree;
		}

		public void Bind(Character character)
		{
			Blackboard sharedBb = Instantiate(blackboard);
			Traverse(rootNode, node =>
			{
				node.Character = character;
				node.Blackboard = sharedBb;
			});
		}

		private void Traverse(Node.Node node, Action<Node.Node> visiter)
		{
			if (node)
			{
				visiter.Invoke(node);
				List<Node.Node> children = GetChildren(node);
				children.ForEach((n) => Traverse(n, visiter));
			}
		}

		public Node.Node.State Update()
		{
			if (rootNode.CurrentState == Node.Node.State.Running)
			{
				treeState = rootNode.Update();
			}

			return treeState;
		}

		public Node.Node CreateNode(Type type)
		{
			Node.Node node = CreateInstance(type) as Node.Node;
			node.name = type.Name;
			node.GenerateGUID();

			Undo.RecordObject(this, "Behaviour Tree (Create node)");

			nodes.Add(node);

			if (!Application.isPlaying)
				AssetDatabase.AddObjectToAsset(node, this);

			AssetDatabase.SaveAssets();
			Undo.RegisterCreatedObjectUndo(node, "Behaviour Tree (Create node)");

			return node;
		}

		public void DeleteNode(Node.Node node)
		{
			Undo.RecordObject(this, "Behaviour Tree (Delete Node)");
			nodes.Remove(node);
			// AssetDatabase.RemoveObjectFromAsset(node);
			Undo.DestroyObjectImmediate(node);
			AssetDatabase.SaveAssets();
		}


		public void AddChild(Node.Node parent, Node.Node child)
		{

			RootNode node = parent as RootNode;
			if (node)
			{
				Undo.RecordObject(node, "Behaviour Tree (add child)");
				node.child = child;
				EditorUtility.SetDirty(node);
			}

			DecoratorNode decoratorNode = parent as DecoratorNode;
			if (decoratorNode)
			{
				Undo.RecordObject(decoratorNode, "Behaviour Tree (add child)");
				decoratorNode.child = child;
				EditorUtility.SetDirty(decoratorNode);
			}

			CompositeNode compositeNode = parent as CompositeNode;
			if (compositeNode)
			{
				Undo.RecordObject(compositeNode, "Behaviour Tree (add child)");
				compositeNode.children.Add(child);
				EditorUtility.SetDirty(compositeNode);
			}
		}


		public void RemoveChild(Node.Node parent, Node.Node child)
		{
			RootNode rootNode = parent as RootNode;
			if (rootNode)
			{
				Undo.RecordObject(rootNode, "Behaviour Tree (remove child)");
				rootNode.child = null;
				EditorUtility.SetDirty(rootNode);
			}

			DecoratorNode decoratorNode = parent as DecoratorNode;
			if (decoratorNode)
			{
				Undo.RecordObject(decoratorNode, "Behaviour Tree (remove child)");
				decoratorNode.child = null;
				EditorUtility.SetDirty(decoratorNode);
			}

			CompositeNode compositeNode = parent as CompositeNode;
			if (compositeNode)
			{
				Undo.RecordObject(compositeNode, "Behaviour Tree (remove child)");
				compositeNode.children.Remove(child);
				EditorUtility.SetDirty(compositeNode);
			}

		}

		public List<Node.Node> GetChildren(Node.Node parent)
		{
			List<Node.Node> children = new List<Node.Node>();

			RootNode rootNode = parent as RootNode;
			if (rootNode && rootNode.child != null)
			{
				children.Add(rootNode.child);
			}

			DecoratorNode decoratorNode = parent as DecoratorNode;
			if (decoratorNode && decoratorNode.child != null)
			{
				children.Add(decoratorNode.child);
			}

			CompositeNode compositeNode = parent as CompositeNode;
			if (compositeNode)
			{
				return compositeNode.children;
			}

			return children;
		}
	}
}
