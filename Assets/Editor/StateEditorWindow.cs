using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class StateEditorWindow : EditorWindow
{
	private StateGraphView graphView;
	private State LoadedState;

	[MenuItem("DeckBuilder/State Editor")]
	public static void ShowWindow()
	{
		StateEditorWindow window = GetWindow<StateEditorWindow>("StateEditor");
		window.minSize = new Vector2(700, 500);
	}

	private void OnEnable()
	{
		graphView = new StateGraphView()
		{
			name = "State graph view",
		};

		//Button saveButton = new Button(SaveStates) { text = "Save" };
		//saveButton.style.marginTop = 10;


		VisualElement ui = new VisualElement();
		ui.Add(graphView);

		rootVisualElement.Add(graphView);
		CreateToolbar();

		//rootVisualElement.Add(saveButton);
	}

	private void CreateToolbar()
	{
		Toolbar toolbar = new Toolbar();

		Button saveButton = new Button(() => SaveStates());
		saveButton.text = "Save";
		toolbar.Add(saveButton);

		rootVisualElement.Add(toolbar);
	}

	private void SaveStates()
	{
		BaseStateNode baseNode = graphView.BaseStateNode;
		if (baseNode != null)
		{
			State baseState = baseNode.State;
			baseState.NodePosition = baseNode.GetPosition();

			if (baseState != null)
			{
				baseState.SubStates.Clear();

				CollectSubStates(baseNode, baseState);

				EditorUtility.SetDirty(baseState);
				AssetDatabase.SaveAssets();

				Debug.Log($"States and sub-states saved for {baseState.name}.");
			}
			else
			{
				Debug.LogWarning("No valid base state to save.");
			}
		}
	}

	private void CollectSubStates(StateNode parentNode, State parentState)
	{
		if (parentNode == null || parentState == null) return;

		foreach (var outputPort in parentNode.outputContainer.Query<Port>().ToList())
		{
			IEnumerable<Edge> edges = outputPort.connections;

			foreach (Edge edge in edges)
			{
				if (edge.input.node is StateNode connectedNode)
				{
					State connectedSubState = connectedNode.State;
					connectedSubState.SubStates.Clear();

					if (connectedSubState != null)
					{
						parentState.SubStates.Add(connectedSubState);

						connectedSubState.ParentState = parentState;
						connectedSubState.NodePosition = connectedNode.GetPosition();

						CollectSubStates(connectedNode, connectedSubState);
					}
				}
			}
		}
	}

	public void LoadState(State selectedState)
	{
		LoadedState = selectedState;

		if (graphView != null)
		{
			LoadStateInGraph(LoadedState);
		}
	}

	private void LoadStateInGraph(State state)
	{
		graphView.DeleteElements(graphView.graphElements.ToList());

		AddBaseNode(state);
	}

	private void AddBaseNode(State state)
	{
		BaseStateNode baseStateNode = new BaseStateNode(state);

		graphView.BaseStateNode = baseStateNode;
		baseStateNode.SetPosition(state.NodePosition);
		graphView.AddElement(baseStateNode);

		AddSubStateNodes(state, baseStateNode);
	}

	private void AddSubStateNodes(State parentState, StateNode parentNode)
	{
		foreach (State subState in parentState.SubStates)
		{
			StateNode subStateNode = new StateNode(subState);

			subStateNode.SetPosition(subState.NodePosition);

			graphView.AddElement(subStateNode);

			Port outputPort = parentNode.outputContainer.Q<Port>();
			Port inputPort = subStateNode.inputContainer.Q<Port>();

			if (outputPort != null && inputPort != null)
			{
				Edge edge = outputPort.ConnectTo(inputPort);
				graphView.AddElement(edge);
			}

			AddSubStateNodes(subState, subStateNode);
		}
	}
}
