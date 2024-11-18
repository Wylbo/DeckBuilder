using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SensorManager : MonoBehaviour
{
    public enum TickMode
    {
        Update,
        FixedUpdate,
        LateUpdate,
        None,
    }

    [SerializeField] private List<Sensor> sensors = new List<Sensor>();
    [SerializeField] public TickMode tickMode;

    private void Awake()
    {
        foreach (Sensor sensor in sensors)
        {
            sensor.Configure();
        }
    }

    private void Update()
    {
        if (tickMode == TickMode.Update)
            Tick();
    }

    private void FixedUpdate()
    {
        if (tickMode == TickMode.FixedUpdate)
            Tick();
    }

    private void LateUpdate()
    {
        if (tickMode == TickMode.LateUpdate)
            Tick();
    }

    public void Tick()
    {
        foreach (Sensor sensor in sensors)
        {
            sensor.Scan();
        }
    }

    public bool HasSensedTarget(out GameObject target)
    {
        target = null;

        foreach (Sensor sensor in sensors)
        {
            if (sensor.HasTarget())
            {
                target = sensor.Target;
                return true;
            }
        }

        return false;
    }
}