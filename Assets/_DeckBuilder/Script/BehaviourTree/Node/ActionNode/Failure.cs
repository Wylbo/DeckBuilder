
namespace BehaviourTree.Node.ActionNode
{
	public class Failure : ActionNode
	{
		protected override void OnStart()
		{
		}

		protected override void OnStop()
		{
		}

		protected override State OnUpdate()
		{
			return State.Failure;
		}
	}
}
