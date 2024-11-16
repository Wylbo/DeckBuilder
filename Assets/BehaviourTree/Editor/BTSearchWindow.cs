using System;
using System.Collections.Generic;
using BehaviourTree.Nodes.ActionNode;
using BehaviourTree.Nodes.CompositeNode;
using BehaviourTree.Nodes.DecoratorNode;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace BehaviourTree.Editor
{
    public class BTSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private BehaviourTreeGraphView graphView;
        public EditorWindow editorWindow;

        public void Initialize(BehaviourTreeGraphView graphView, EditorWindow editorWindow)
        {
            this.graphView = graphView;
            this.editorWindow = editorWindow;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> searchTreeEntries = new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Element")),
            };
            PopulateSearchTree(searchTreeEntries, typeof(ActionNode));
            PopulateSearchTree(searchTreeEntries, typeof(CompositeNode));
            PopulateSearchTree(searchTreeEntries, typeof(DecoratorNode));
            return searchTreeEntries;
        }

        private void PopulateSearchTree(List<SearchTreeEntry> searchTreeEntries, Type T)
        {
            TypeCache.TypeCollection typeCollection = TypeCache.GetTypesDerivedFrom(T);

            searchTreeEntries.Add(new SearchTreeGroupEntry(new GUIContent($"[{T.Name}]"), 1));

            foreach (Type type in typeCollection)
            {
                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(type.Name));
                entry.level = 2;
                entry.userData = type;

                searchTreeEntries.Add(entry);
            }
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            Vector2 windowMousePosition = editorWindow.rootVisualElement.ChangeCoordinatesTo(editorWindow.rootVisualElement.parent,
            context.screenMousePosition - editorWindow.position.position);
            Vector2 graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);
            graphView.CreateNode(entry.userData as Type, graphMousePosition);
            return true;
        }

    }
}
