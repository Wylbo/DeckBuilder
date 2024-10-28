using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityDash), menuName = FileName.Ability + nameof(AbilityDash))]
public class AbilityDash : Ability
{
	[SerializeField]
	private Movement.DashData dashData;

	public override void Cast()
	{
		Movement movement = Caster.GetComponent<Movement>();
		movement.Dash(dashData);
	}


}
