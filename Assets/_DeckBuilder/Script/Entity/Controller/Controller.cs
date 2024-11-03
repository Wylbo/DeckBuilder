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

	protected virtual void Start()
	{
		controlStrategy?.Initialize(this);
	}

	protected virtual void Update()
	{
		controlStrategy?.Control();
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
}
