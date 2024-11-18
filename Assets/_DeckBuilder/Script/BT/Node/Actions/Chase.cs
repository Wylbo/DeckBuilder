using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

[System.Serializable]
public class Chase : ActionNode
{
    [SerializeField] private NodeProperty<float> maxDistanceFromTarget;
    [SerializeField] private NodeProperty<float> stopDistance;
    [SerializeField] private NodeProperty<GameObject> toChase;

    private Movement movement;
    private float prevStopDistance;

    protected override void OnStart()
    {
        movement = context.GetComponent<Movement>();

        prevStopDistance = movement.Agent.stoppingDistance;
        movement.Agent.stoppingDistance = stopDistance.Value;

        movement.MoveTo(toChase.Value.transform.position);
    }

    protected override void OnStop()
    {
        movement.Agent.stoppingDistance = prevStopDistance;
    }

    protected override State OnUpdate()
    {
        movement.Agent.stoppingDistance = stopDistance.Value;
        movement.MoveTo(toChase.Value.transform.position);

        if (movement.Agent.remainingDistance < stopDistance.Value)
            return State.Success;

        if (movement.Agent.remainingDistance >= maxDistanceFromTarget.Value)
            return State.Failure;

        return State.Running;
    }
}
