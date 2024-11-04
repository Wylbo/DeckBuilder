using System;

public abstract class DebuffApplier
{
    protected float tickRate = 1f;
    protected Timer duration;
    protected int effectStacks;
    public ScriptableDebuff Debuff { get; }
    protected DebuffUpdater debuffUpdater;
    private float timeSinceLastTick = 0f;
    public event Action<DebuffApplier> On_Ended;

    public DebuffApplier(ScriptableDebuff debuff, DebuffUpdater debuffUpdater)
    {
        Debuff = debuff;
        tickRate = debuff.Tickrate;

        this.debuffUpdater = debuffUpdater;
        duration = new Timer(debuff.Duration);
    }

    public void Tick(float dt)
    {
        duration.Update(dt);

        timeSinceLastTick += dt;

        if (timeSinceLastTick > tickRate)
        {
            timeSinceLastTick -= tickRate;
            ApplyTick();
        }
    }

    private void Duration_On_Ended()
    {
        On_Ended?.Invoke(this);
        duration.On_Ended -= Duration_On_Ended;
        End();
    }

    public void Activate()
    {
        duration.On_Ended += Duration_On_Ended;
        if (Debuff.StackingPolicy == DebuffStackingPolicy.Stack)
        {
            ApplyEffect();
            effectStacks++;
        }
        if (Debuff.StackingPolicy == DebuffStackingPolicy.Refresh)
        {
            RefreshEffect();
        }
        if (Debuff.DurationPolicy == DebuffDurationPolicy.Additive)
        {
            duration.AddTime(Debuff.Duration);
        }
        if (Debuff.DurationPolicy == DebuffDurationPolicy.Refresh)
        {
            duration.Cancel();
            duration.Start();
        }
    }

    private void RefreshEffect()
    {
        duration.On_Ended -= Duration_On_Ended;
        End();
        ApplyEffect();
    }

    protected abstract void ApplyEffect();
    protected abstract void ApplyTick();
    public abstract void End();
}