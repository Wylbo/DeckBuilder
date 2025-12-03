using UnityEngine;
using TheKiwiCoder;
using UnityEngine.AI;

[System.Serializable]
public class UpdateSensors : ActionNode
{
    private SensorManager sensorManager;

    protected override void OnStart()
    {
        sensorManager = context.gameObject.GetComponent<SensorManager>();
        sensorManager.tickMode = SensorManager.TickMode.None;
    }

    protected override void OnStop()
    {

    }

    protected override State OnUpdate()
    {
        sensorManager.Tick();
        return State.Running;
    }
}