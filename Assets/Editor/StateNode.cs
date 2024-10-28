using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using UnityEditor;
using UnityEditor.Search;

public class StateNode : Node
{
	public State State;

	private Vector2 previousPos;

	public StateNode(State state)
	{
		State = state;


		title = State?.name;
		SetPosition(new Rect(100, 100, 200, 150));
		CreateInputPort();
		CreateOutputPort();

		ObjectField stateObjectField = new ObjectField("State")
		{
			objectType = typeof(State),
			value = state,
		};

		stateObjectField.RegisterValueChangedCallback(evt =>
		{
			State = evt.newValue as State; 
		});

		mainContainer.Add(stateObjectField);

		RefreshExpandedState();
		RefreshPorts();
	}

	public override void SetPosition(Rect newPos)
	{
		if(newPos.position != previousPos && State != null)
		{
			Undo.RecordObject(State, "Move Node");
			base.SetPosition(newPos);

			previousPos = newPos.position;

			EditorUtility.SetDirty(State);
		} else
		{
			base.SetPosition(newPos);
		}

	}

	protected virtual void CreateOutputPort()
	{
		Port outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(State));
		outputPort.portName = "Childs";
		outputPort.style.flexDirection = FlexDirection.RowReverse;

		outputContainer.Add(outputPort);
	}

	protected virtual void CreateInputPort()
	{
		Port inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(State));

		inputPort.portName = "Parent";
		inputPort.style.flexDirection = FlexDirection.Row;
		inputContainer.Add(inputPort);
	}
}


public class BaseStateNode : StateNode
{
	public BaseStateNode(State state) : base(state)
	{
	}

	protected override void CreateInputPort()
	{
		
	}
}