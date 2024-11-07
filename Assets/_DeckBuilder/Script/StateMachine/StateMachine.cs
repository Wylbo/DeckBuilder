using UnityEngine;

public class StateMachine : MonoBehaviour
{
	[SerializeField]
	private Character owner = null;
	[SerializeField]
	private State rootState = null;
	[SerializeField]
	private State currentState = null;
	[SerializeField]
	private State newState = null;


	public Character Owner => owner;
	public State DefaultState => rootState;

	public State CurrentState => currentState;

	public State NewState => newState;

	private void Start()
	{
		SetState(rootState);
	}

	private void Update()
	{
		rootState?.Update();
	}

	public void SetState(State state)
	{
		rootState?.Exit();
		rootState = state?.Enter(this);
		currentState = GetActualCurrentState();
	}

	private State GetActualCurrentState()
	{
		State evaluatedState = rootState;
		if (evaluatedState == null)
			return null;

		while (evaluatedState.CurrentSubState != evaluatedState)
		{
			evaluatedState = evaluatedState.CurrentSubState;
		}

		return evaluatedState;

	}

}
