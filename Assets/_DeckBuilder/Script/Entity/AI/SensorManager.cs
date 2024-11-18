using System.Collections.Generic;
using UnityEngine;

public class SensorManager : MonoBehaviour
{
    private enum TickMode
    {
        Update,
        FixedUpdate,
        LateUpdate,
        None,
    }

    [SerializeField] private List<Sensor> sensors = new List<Sensor>();
    [SerializeField] private TickMode tickMode;

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

    private void Tick()
    {
        foreach (Sensor sensor in sensors)
        {
            sensor.Scan();
        }
    }
}