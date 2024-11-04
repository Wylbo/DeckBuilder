using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ScriptableDebuff : ScriptableObject
{
    [SerializeField]
    private float duration = 0;
    [SerializeField]
    private float tickrate;
    [SerializeField]
    private DebuffStackingPolicy stackingPolicy = DebuffStackingPolicy.Stack;
    [SerializeField]
    private DebuffDurationPolicy durationPolicy = DebuffDurationPolicy.Refresh;

    public DebuffStackingPolicy StackingPolicy => stackingPolicy;
    public DebuffDurationPolicy DurationPolicy => durationPolicy;
    public float Duration => duration;
    public float Tickrate => tickrate;
    public abstract DebuffApplier InitDebuff(DebuffUpdater target);
}