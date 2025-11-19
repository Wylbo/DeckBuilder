using System;
using UnityEngine;

/// <summary>
/// Base class allowing to controll a character
/// </summary>
[RequireComponent(typeof(Character))]
public class Controller : MonoBehaviour
{
	[SerializeField]
	private Character character;

	[SerializeField]
	private ControlStrategy controlStrategy;

	// Runtime instance to avoid shared SO state across multiple controllers
	private ControlStrategy runtimeControlStrategy;

	protected virtual void Start()
	{
		if (controlStrategy != null)
		{
			// Instantiate a per-controller copy so per-instance fields aren't shared
			runtimeControlStrategy = Instantiate(controlStrategy);
			runtimeControlStrategy.Initialize(this, character);
		}
	}

	protected virtual void Update()
	{
		runtimeControlStrategy?.Control(Time.deltaTime);
	}

	protected virtual void OnDestroy()
	{
		runtimeControlStrategy?.Disable();
	}

	private void Reset()
	{
		character = GetComponent<Character>();
	}

	public bool TryMove(Vector3 worldTo)
	{
		return character.MoveTo(worldTo);
	}

	public void CastAbility(int index, Vector3 worldPos)
	{
		character.CastAbility(index, worldPos);
	}

	public void EndHold(int index, Vector3 worldPos)
	{
		character.EndHold(index, worldPos);
	}

	public void PerformDodge(Vector3 worldPos)
	{
		character.PerformDodge(worldPos);
	}
}
