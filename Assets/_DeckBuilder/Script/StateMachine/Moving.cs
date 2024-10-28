using UnityEngine;

[CreateAssetMenu(fileName = nameof(Moving), menuName = FileName.State + nameof(Moving))]
public class Moving : State
{
	[SerializeField]
	private State idleState = null;
	public override void Update()
	{
		base.Update();
		if(!machine.Owner.Movement.isMoving)
		{
			machine.SetState(idleState);
		}

	}
}
