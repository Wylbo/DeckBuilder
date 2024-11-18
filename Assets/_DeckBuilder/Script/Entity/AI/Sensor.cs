using System.Collections.Generic;
using UnityEngine;

public abstract class Sensor : MonoBehaviour, ISensor
{
    [SerializeField] protected SensorConfig config;

    protected GameObject target;
    public GameObject Target => target;

    public void Configure()
    {
        Configure(config);
    }

    public void Configure(SensorConfig config)
    {
        this.config = config;
    }

    public virtual void Scan()
    {
        target = null;
    }

    public bool HasTarget()
    {
        return target != null;
    }
}