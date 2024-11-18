using UnityEditor;
using UnityEngine;

public class VisualSensor : Sensor
{
    [SerializeField] private float fieldOfView;

    public override void Scan()
    {
        Collider[] targetInRange = new Collider[10];
        int foundTargets = Physics.OverlapSphereNonAlloc(config.Origin.transform.position, config.Radius, targetInRange, config.TargetLayer);

        for (int i = 0; i < foundTargets; i++)
        {
            GameObject target = targetInRange[i].gameObject;

            Vector3 dirToTarget = (target.transform.position - config.Origin.position).normalized;
            float angleToTarget = Vector3.Angle(config.Origin.forward, dirToTarget);

            if (angleToTarget > fieldOfView / 2)
                return;

            if (Physics.Raycast(config.Origin.position, dirToTarget, out RaycastHit hit, config.Radius, config.TargetLayer))
            {
                Debug.Log($"[{nameof(VisualSensor)}] target detected: {target.name}");
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!config.Origin)
            return;

        Handles.color = new Color(0, 0, 1, 0.2f);

        Handles.DrawSolidArc(
            config.Origin.transform.position,
            Vector3.up,
            Quaternion.Euler(0, -fieldOfView / 2, 0) * config.Origin.forward,
            fieldOfView,
            config.Radius
        );
    }
#endif
}