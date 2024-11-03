using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A <see cref="Debuff"/> can be a positif or a neganif effect.
/// It's updated each frame by the <see cref="DebuffUpdater"/>
/// </summary>
public abstract class Debuff : ScriptableObject
{
    [SerializeField]
    private Timer duration = new Timer(0);

    [SerializeField]
    private DebuffStackingPolicy stackingPolicy = DebuffStackingPolicy.Stack;

    public DebuffStackingPolicy StackingPolicy => stackingPolicy;

    public void Init()
    {
        if (duration.TotalTime > 0)
        {
            duration.Start();
            duration.On_Ended += Duration_On_Ended;
        }
    }

    private void Duration_On_Ended()
    {
        Remove();
    }

    public void Update()
    {
        if (duration.TotalTime > 0)
            duration.Update(Time.deltaTime);

        UpdateDebuff();
    }

    /// <summary>
    /// update the actual debuff like ticking poison damage over time
    /// </summary>
    protected abstract void UpdateDebuff();

    protected void Remove()
    {
        // TODO : send event to remove
    }
}