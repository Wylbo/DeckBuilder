using System.Linq;
using BehaviourTree.Nodes;
using BehaviourTree.Nodes.CompositeNode;
using UnityEngine;

public class Parallel : CompositeNode
{
    private enum Policy { AllMustSucceed, AnyCanSucceed }

    [SerializeField] private Policy successPolicy;

    protected override void OnStart()
    {
    }

    protected override void OnStop()
    {
    }

    protected override State OnUpdate()
    {
        bool anyChildIsRunning = false;

        foreach (Node n in children)
        {
            State childState = n.Update();
            if (childState == State.Running)
                anyChildIsRunning = true;
            else if (childState == State.Success && successPolicy == Policy.AnyCanSucceed)
                return State.Success;
            else if (childState == State.Failure && successPolicy == Policy.AllMustSucceed)
                return State.Failure;
        }

        if (!anyChildIsRunning)
        {
            bool allSucceeded = children.All(c => c.CurrentState == State.Success);
            if (allSucceeded && successPolicy == Policy.AllMustSucceed)
                return State.Success;
            else
                return State.Failure;
        }

        return State.Running;
    }
}
