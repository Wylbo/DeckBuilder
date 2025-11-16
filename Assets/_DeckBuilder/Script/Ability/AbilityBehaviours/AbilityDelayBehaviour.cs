using System;
using UnityEngine;

[Serializable]
public class AbilityDelayBehaviour : AbilityBehaviour
{
    [SerializeField, Min(0f)] private float delaySeconds = 0.25f;

    public float DelaySeconds => Mathf.Max(0f, delaySeconds);

    // No-op hooks; sequencing is handled by Ability runtime.
}

