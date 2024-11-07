using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Linq;
using System;

public class StateGraphView : GraphView
{
	public BaseStateNode BaseStateNode { get; set; }


	public StateGraphView()
	{
		GridBackground gridBackground = new GridBackground();
		Insert(0, gridBackground);

		styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/GraphStyle.uss"));
		this.AddManipulator(new ContentDragger());
		this.AddManipulator(new SelectionDragger());
		this.AddManipulator(new RectangleSelector());
		this.AddManipulator(CreateGroupContextualMenu());

		this.StretchToParentSize();

		SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

		RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
		RegisterCallback<DragPerformEvent>(OnDragPerformed);

		AddBaseStateNode();

	}

	private IManipulator CreateGroupContextualMenu()
	{
		ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
		   menuEvent => menuEvent.menu.AppendAction("Add Group", actionEvent => AddElement(CreateGroup("group", actionEvent.eventInfo.localMousePosition)))
		);

		return contextualMenuManipulator;
	}

	private Group CreateGroup(string title, Vector2 eventInfo)
	{
		Group group = new Group()
		{
			title = title
		};

		group.SetPosition(new Rect(GetLocalMousePosition(eventInfo), Vector2.zero));
		return group;
	}

	public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
	{
		return ports.Where(p =>
		p.direction != startPort.direction &&
		p.portType == startPort.portType).ToList();
	}


	private void OnDragPerformed(DragPerformEvent evt)
	{
		Vector2 mousePos = evt.localMousePosition;

		if (DragAndDrop.objectReferences.Length > 0 && DragAndDrop.objectReferences[0] is State droppedState)
		{
			DragAndDrop.AcceptDrag();
			AddStateNode(droppedState, GetLocalMousePosition(mousePos));
		}
	}

	private void OnDragUpdated(DragUpdatedEvent evt)
	{
		if (DragAndDrop.objectReferences.Length > 0)
		{
			if (DragAndDrop.objectReferences[0] is State)
			{
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			}
		}
	}


	private void AddStateNode(State state = null, Vector2 position = default)
	{
		StateNode stateNode = new StateNode(state)
		{
			title = state != null ? state.name : "New State",
			style = { left = position.x, top = position.y },
		};
		AddElement(stateNode);
	}

	private void AddBaseStateNode(State state = null, Vector2 position = default)
	{
		BaseStateNode = new BaseStateNode(state) { title = "Base State" };
		AddElement(BaseStateNode);

	}

	public Vector2 GetLocalMousePosition(Vector2 mousePosition)
	{
		Vector2 worldMousePos = mousePosition;
		Vector2 localMousepos = contentViewContainer.WorldToLocal(worldMousePos);

		return localMousepos;
	}
}
