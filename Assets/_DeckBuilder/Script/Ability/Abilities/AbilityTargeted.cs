using System;
using UnityEngine;

public abstract class AbilityTargeted : Ability
{
    [SerializeField] private float autoTargetingRange = 0.5f;
    [SerializeField] private LayerMask targetableLayerMask;

    private static readonly Collider[] overlapBuffer = new Collider[16];

    protected override void DoCast(Vector3 worldPos)
    {
        int hits = Physics.OverlapSphereNonAlloc(
            worldPos,
            autoTargetingRange,
            overlapBuffer,
            targetableLayerMask
        );

        Targetable closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < hits; i++)
        {
            Collider col = overlapBuffer[i];
            if (col.TryGetComponent(out Targetable target))
            {
                float sqrDist = (target.transform.position - worldPos).sqrMagnitude;
                if (sqrDist < closestDist)
                {
                    closest = target;
                    closestDist = sqrDist;
                }
            }
        }

        if (closest != null)
            DoTargetedAbility(closest);
        else
            DoTargetAbilityAtCursorPos(worldPos);

        base.DoCast(worldPos);
    }

    protected abstract void DoTargetAbilityAtCursorPos(Vector3 worldPos);

    protected abstract void DoTargetedAbility(Targetable target);
}