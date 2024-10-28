using UnityEngine;

[CreateAssetMenu(fileName = nameof(IdleState), menuName = FileName.State + nameof(IdleState))]
public class IdleState : State
{
	[SerializeField]
	private AnimationClip IdleAnimation;
	[SerializeField]
	private State MovingState;

	public override State Enter(StateMachine stateMachine)
	{
		return base.Enter(stateMachine);
	}

	public override void Update()
	{
		base.Update();

		if (machine.Owner.Movement.isMoving)
		{
			machine.SetState(MovingState);
		}
	}
}