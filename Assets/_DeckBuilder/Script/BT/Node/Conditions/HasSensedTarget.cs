using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

[System.Serializable]
public class HasSensedTarget : ConditionNode
{
    [SerializeField] private NodeProperty<GameObject> sensedTarget;
    [SerializeField] private NodeProperty<Vector3> sensedTargetPosition;
    private SensorManager sensorManager;

    protected override void OnStart()
    {
        base.OnStart();
        sensorManager = context.GetComponent<SensorManager>();
    }

    protected override bool CheckCondition()
    {
        bool hasSensedTarget = sensorManager.HasSensedTarget(out GameObject target);

        sensedTarget.Value = target;
        if (sensedTarget.Value)
            sensedTargetPosition.Value = target.transform.position;


        return hasSensedTarget;
    }
}
