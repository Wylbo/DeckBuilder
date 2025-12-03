using System;
using System.Collections.Generic;

public class DebuffApplier
{
    private readonly ScriptableDebuff debuff;
    private readonly DebuffUpdater debuffUpdater;
    private readonly Timer duration;
    private readonly List<AbilityModifier> appliedAbilityModifiers = new();
    private readonly List<GlobalModifier> appliedGlobalModifiers = new();

    private int effectStacks;
    private bool isRemoving;

    public DebuffApplier(ScriptableDebuff debuff, DebuffUpdater debuffUpdater)
    {
        this.debuff = debuff;
        this.debuffUpdater = debuffUpdater;
        duration = new Timer(debuff != null ? debuff.Duration : 0f);
    }

    public event Action<DebuffApplier> On_Ended;

    public ScriptableDebuff Debuff => debuff;
    public int CurrentStacks => effectStacks;
    public float RemainingDuration => duration?.Remaining ?? 0f;
    public float TotalDuration => duration?.TotalTime ?? 0f;
    public bool IsDurationRunning => duration != null && duration.IsRunning;

    private StatsModifierManager ModifierManager => debuffUpdater != null ? debuffUpdater.ModifierManager : null;

    public void Tick(float deltaTime)
    {
        duration.Update(deltaTime);
    }

    public void Activate()
    {
        if (debuff == null)
            return;

        duration.On_Ended -= Duration_On_Ended;
        duration.On_Ended += Duration_On_Ended;

        switch (debuff.StackingPolicy)
        {
            case DebuffStackingPolicy.Stack:
                ApplyEffect();
                effectStacks++;
                break;
            case DebuffStackingPolicy.Refresh:
                RefreshEffect();
                effectStacks = 1;
                break;
            case DebuffStackingPolicy.Single:
                if (effectStacks == 0)
                {
                    ApplyEffect();
                    effectStacks = 1;
                }
                break;
        }

        switch (debuff.DurationPolicy)
        {
            case DebuffDurationPolicy.Additive:
                if (duration.IsRunning)
                    duration.AddTime(debuff.Duration);
                else
                    duration.Start(debuff.Duration);
                break;
            case DebuffDurationPolicy.Refresh:
                duration.Cancel();
                duration.Start(debuff.Duration);
                break;
            default:
                if (!duration.IsRunning)
                    duration.Start(debuff.Duration);
                break;
        }
    }

    public void Remove()
    {
        RemoveInternal(stopTimer: true);
    }

    private void Duration_On_Ended()
    {
        RemoveInternal(stopTimer: false);
    }

    private void RefreshEffect()
    {
        RemoveConfiguredModifiers();
        ApplyEffect();
    }

    private void ApplyEffect()
    {
        ApplyConfiguredModifiers();
    }

    private void ApplyConfiguredModifiers()
    {
        var manager = ModifierManager;
        if (manager == null || debuff == null)
            return;

        if (debuff.AbilityModifiers != null)
        {
            foreach (var modifier in debuff.AbilityModifiers)
            {
                if (modifier == null)
                    continue;

                manager.AddAbilityModifier(modifier);
                appliedAbilityModifiers.Add(modifier);
            }
        }

        if (debuff.GlobalModifiers != null)
        {
            foreach (var modifier in debuff.GlobalModifiers)
            {
                if (modifier == null)
                    continue;

                manager.AddGlobalModifier(modifier);
                appliedGlobalModifiers.Add(modifier);
            }
        }
    }

    private void RemoveConfiguredModifiers()
    {
        var manager = ModifierManager;

        if (manager != null)
        {
            foreach (var modifier in appliedAbilityModifiers)
                manager.RemoveAbilityModifier(modifier);

            foreach (var modifier in appliedGlobalModifiers)
                manager.RemoveGlobalModifier(modifier);
        }

        appliedAbilityModifiers.Clear();
        appliedGlobalModifiers.Clear();
    }

    private void RemoveInternal(bool stopTimer)
    {
        if (isRemoving)
            return;

        isRemoving = true;

        duration.On_Ended -= Duration_On_Ended;

        if (stopTimer)
            duration.Stop();

        RemoveConfiguredModifiers();
        On_Ended?.Invoke(this);
    }
}
