using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = nameof(State), menuName = FileName.State)]
public class State : ScriptableObject
{
	[SerializeField]
	private List<State> subStates = new List<State>();

	protected StateMachine machine;

	protected State currentSubState;

	public State CurrentSubState => currentSubState;


	public State ParentState { get; set; }
	public List<State> SubStates => subStates;

#if UNITY_EDITOR

	public Rect NodePosition;

#endif

	protected virtual void OnEnable()
	{
		currentSubState = this;
	}

	#region State
	public virtual State Enter(StateMachine stateMachine)
	{
		machine = stateMachine;

		Stack<State> parentStack = new Stack<State>();
		parentStack = GetParentPath(parentStack);

		return Enter_Internal(stateMachine, parentStack);
	}

	protected virtual State Enter_Internal(StateMachine stateMachine, Stack<State> parentStack)
	{
		State root = parentStack.Pop();

		EnterChildState(stateMachine, root, parentStack);

		return root;
	}

	private void EnterChildState(StateMachine stateMachine, State parent, Stack<State> parentStack)
	{
		parent.machine = stateMachine;

		if (parentStack.Count > 0)
		{
			parent.currentSubState = parentStack.Pop();

			EnterChildState(stateMachine, parent.currentSubState, parentStack);
			return;
		}

		if (subStates.Count <= 0)
			currentSubState = this;
		else
		{
			currentSubState = subStates[0];
			currentSubState.Enter(stateMachine);
		}
	}

	public virtual void Exit()
	{
		if (currentSubState != this)
		{
			currentSubState.Exit();
		}

		currentSubState = null;
	}

	public virtual void Update()
	{
		if (currentSubState != this)
			currentSubState.Update();
	}

	private Stack<State> GetParentPath(Stack<State> parentStack)
	{
		parentStack.Push(this);
		if (ParentState)
			return ParentState.GetParentPath(parentStack);

		return parentStack;
	}
	#endregion
}
