using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(BehaviourTree), menuName = FileName.BehaviourTree + nameof(BehaviourTree))]
public class BehaviourTree : ScriptableObject
{
    public Node rootNode;
    public Node.State treeState = Node.State.Running;

    public List<Node> nodes = new List<Node>();

    public BehaviourTree Clone()
    {
        BehaviourTree tree = Instantiate(this);
        tree.rootNode = tree.rootNode.Clone();
        return tree;
    }

    public Node.State Update()
    {
        if (rootNode.state == Node.State.Running)
        {
            treeState = rootNode.Update();
        }

        return treeState;
    }

    public Node CreateNode(System.Type type)
    {
        Node node = CreateInstance(type) as Node;
        node.name = type.Name;
        node.guid = GUID.Generate().ToString();

        Undo.RecordObject(this, "Behaviour Tree (Create node)");

        nodes.Add(node);

        AssetDatabase.AddObjectToAsset(node, this);
        AssetDatabase.SaveAssets();
        Undo.RegisterCreatedObjectUndo(node, "Behaviour Tree (Create node)");

        return node;
    }

    public void DeleteNode(Node node)
    {
        Undo.RecordObject(this, "Behaviour Tree (Delete Node)");
        nodes.Remove(node);
        // AssetDatabase.RemoveObjectFromAsset(node);
        Undo.DestroyObjectImmediate(node);
        AssetDatabase.SaveAssets();
    }


    public void AddChild(Node parent, Node child)
    {

        RootNode rootNode = parent as RootNode;
        if (rootNode)
        {
            Undo.RecordObject(rootNode, "Behaviour Tree (add child)");
            rootNode.child = child;
            EditorUtility.SetDirty(rootNode);
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


    public void RemoveChild(Node parent, Node child)
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

    public List<Node> GetChildren(Node parent)
    {
        List<Node> children = new List<Node>();

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
