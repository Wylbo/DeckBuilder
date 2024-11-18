using UnityEngine;

public interface ISensor
{
    public void Configure(SensorConfig config);

    public void Scan();

    public bool HasTarget();
}
