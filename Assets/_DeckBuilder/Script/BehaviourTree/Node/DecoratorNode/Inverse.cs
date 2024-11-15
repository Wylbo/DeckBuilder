
namespace BehaviourTree.Node.DecoratorNode
{
	public class Inverse : DecoratorNode
	{
		protected override void OnStart()
		{
		}

		protected override void OnStop()
		{
		}

		protected override State OnUpdate()
		{
			return child.Update() switch
			{
				State.Running => State.Running,
				State.Failure => State.Success,
				State.Success => State.Failure,
				_ => State.Failure,
			};
		}
	}
}