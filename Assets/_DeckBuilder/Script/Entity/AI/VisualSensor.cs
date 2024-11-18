using UnityEditor;
using UnityEngine;

public class VisualSensor : Sensor
{
    [SerializeField] private float fieldOfView;

    public override void Scan()
    {
        base.Scan();

        Collider[] targetsInRange = new Collider[1];
        int foundTargets = Physics.OverlapSphereNonAlloc(config.Origin.transform.position, config.Radius, targetsInRange, config.TargetLayer);

        for (int i = 0; i < foundTargets; i++)
        {
            GameObject potentialTarget = targetsInRange[i].gameObject;

            Vector3 dirToTarget = (potentialTarget.transform.position - config.Origin.position).normalized;
            float angleToTarget = Vector3.Angle(config.Origin.forward, dirToTarget);

            if (angleToTarget > fieldOfView / 2)
                return;

            if (Physics.Raycast(config.Origin.position, dirToTarget, out RaycastHit hit, config.Radius, config.TargetLayer))
            {
                target = potentialTarget;
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