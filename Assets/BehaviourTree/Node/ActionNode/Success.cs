namespace BehaviourTree.Nodes.ActionNode
{
	public class Success : ActionNode
	{
		protected override void OnStart()
		{
		}

		protected override void OnStop()
		{
		}

		protected override State OnUpdate()
		{
			return State.Success;
		}
	}
}
