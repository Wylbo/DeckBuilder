using UnityEngine;

namespace BehaviourTree.Node.DecoratorNode
{
	public class Repeat : DecoratorNode
	{
		[SerializeField] private int loops;

		private int elapsedLoops = 0;
		protected override void OnStart()
		{
			elapsedLoops = 0;
		}

		protected override void OnStop()
		{

		}

		protected override State OnUpdate()
		{
			if (loops > 0 && elapsedLoops <= loops && child.CurrentState == State.Success)
			{
				elapsedLoops++;
				Debug.Log($"todo loops: {loops}, loop done: {elapsedLoops}");
			}
			if (loops > 0 && elapsedLoops >= loops)
			{
				return State.Success;
			}
			child.Update();
			return State.Running;
		}
	}
}
