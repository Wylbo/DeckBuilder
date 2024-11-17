using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

[System.Serializable]
public class UseAbility : ActionNode
{
    [SerializeField] private NodeProperty<int> abilityIndex;
    [SerializeField] private NodeProperty<Vector3> targetPosition;

    private AbilityCaster caster;
    protected override void OnStart()
    {
        caster = context.GetComponent<AbilityCaster>();
    }

    protected override void OnStop()
    {
    }

    protected override State OnUpdate()
    {
        bool casted = caster.Cast(abilityIndex.Value, targetPosition.Value);
        return casted ? State.Success : State.Failure;
    }
}
