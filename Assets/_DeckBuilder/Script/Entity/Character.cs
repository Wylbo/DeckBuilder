using System;
using UnityEngine;

public class Character : Entity
{
	[SerializeField]
	private Movement movement;
	[SerializeField]
	private AbilityCaster abilityCaster;


	public Movement Movement => movement;

	public bool MoveTo(Vector3 worldTo)
	{
		return movement.MoveTo(worldTo);
	}

	public void CastAbility(int index, Vector3 worldPos)
	{
		abilityCaster.Cast(index, worldPos);
	}
}
