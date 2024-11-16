using UnityEngine;

namespace BehaviourTree.Nodes.CompositeNode
{
    public class Selector : CompositeNode
    {
        private int current;
        protected override void OnStart()
        {
            current = 0;
        }

        protected override void OnStop()
        {
        }

        protected override State OnUpdate()
        {
            Node child = children[current];

            switch (child.Update())
            {
                case State.Running:
                    return State.Running;
                case State.Failure:
                    current++;
                    break;
                case State.Success:
                    return State.Success;
            }

            return current == children.Count ? State.Failure : State.Running;
        }
    }
}