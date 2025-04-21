using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityDash), menuName = FileName.Abilities + nameof(AbilityDash))]
public class AbilityDash : Ability
{
	[SerializeField]
	private Movement.DashData dashData;

	protected override void DoCast(Vector3 worldPos)
	{
		movement.Dash(dashData, worldPos);
		base.DoCast(worldPos);
	}

	protected void DoCast()
	{
		movement.Dash(dashData);
	}


}
