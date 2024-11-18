using UnityEngine;

public abstract class Sensor : MonoBehaviour, ISensor
{
    [SerializeField] protected SensorConfig config;

    public void Configure()
    {
        Configure(config);
    }

    public void Configure(SensorConfig config)
    {
        this.config = config;
    }

    public abstract void Scan();

    public bool IsTargetInRange(GameObject target)
    {
        if (target == null)
            return false;

        float distance = Vector3.Distance(config.Origin.transform.position, target.transform.position);
        return distance < config.Radius && ((1 << target.layer) & config.TargetLayer) != 0;
    }
}