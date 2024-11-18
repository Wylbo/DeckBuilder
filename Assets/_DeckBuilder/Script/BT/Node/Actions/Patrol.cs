using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using UnityEngine.AI;

[System.Serializable]
public class Patrol : ActionNode
{
    [SerializeField] private NodeProperty<List<GameObject>> patrolPoints;

    private Movement movement;
    private NavMeshAgent agent;
    private int currentPatrolPoint;

    protected override void OnStart()
    {
        movement = context.GetComponent<Movement>();
        agent = movement.Agent;
        currentPatrolPoint = 0;
    }

    protected override void OnStop()
    {
    }

    protected override State OnUpdate()
    {
        if (agent == null || !agent.enabled || agent.pathStatus == NavMeshPathStatus.PathInvalid)
            return State.Failure;

        movement.MoveTo(patrolPoints.Value[currentPatrolPoint].transform.position);

        if (agent.remainingDistance < 0.01)
        {
            if (agent.pathPending)
                return State.Running;

            currentPatrolPoint++;

            if (currentPatrolPoint >= patrolPoints.Value.Count)
                return State.Success;
        }
        return State.Running;
    }
}
