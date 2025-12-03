using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IAbilityMovement
{
    bool IsMoving { get; }
    void StopMovement();
    void DisableMovement();
    void EnableMovement();
    void Dash(Movement.DashData dashData, Vector3 toward);
}

public interface IAbilityDebuffService
{
    void AddDebuff(ScriptableDebuff scriptableDebuff);
    void RemoveDebuff(ScriptableDebuff scriptableDebuff);
}

public interface IAbilityStatProvider
{
    Dictionary<AbilityStatKey, float> EvaluateStats(
        GTagSet tagSet,
        IEnumerable<AbilityStatEntry> baseStats,
        IEnumerable<AbilityModifier> activeModifiers,
        IReadOnlyDictionary<GlobalStatKey, float> globalStats);
}

public interface IAbilityExecutor
{
    Ability Definition { get; }
    AbilityCaster Caster { get; }
    IAbilityMovement Movement { get; }
    ProjectileLauncher ProjectileLauncher { get; }
    StatsModifierManager ModifierManager { get; }
    IAbilityStatProvider StatProvider { get; }
    IGlobalStatSource GlobalStatSource { get; }
    IAbilityDebuffService DebuffService { get; }
    event UnityAction<Ability> On_StartCast;
    event UnityAction<bool> On_EndCast;
    float Cooldown { get; }
    bool IsCasting { get; }
    void Cast(Vector3 worldPos, bool isHeld);
    void EndHold(Vector3 worldPos);
    void EndCast(Vector3 worldPos, bool isSuccessful = true);
    void Update(float deltaTime);
    float GetStat(AbilityStatKey key);
    void LookAtCastDirection(Vector3 worldPos);
    void Disable();
}

public sealed class AbilityExecutor : IAbilityExecutor
{
    private readonly List<AbilityBehaviour> behaviours;
    private AbilityBehaviourContext behaviourContext;
    private AbilityCastContext activeCastContext;
    private bool hasPendingStartSequence;
    private float pendingDelayRemaining;
    private int pendingStartIndex;
    private bool requiresUpdate;
    private bool blocksAbilityEnd;

    public Ability Definition { get; }
    public AbilityCaster Caster { get; }
    public IAbilityMovement Movement { get; }
    public ProjectileLauncher ProjectileLauncher { get; }
    public StatsModifierManager ModifierManager { get; }
    public IAbilityStatProvider StatProvider { get; }
    public IGlobalStatSource GlobalStatSource { get; }
    public IAbilityDebuffService DebuffService { get; }
    public bool IsCasting { get; private set; }
    public float Cooldown => GetStat(AbilityStatKey.Cooldown);

    public event UnityAction<Ability> On_StartCast;
    public event UnityAction<bool> On_EndCast;

    public AbilityExecutor(
        Ability ability,
        AbilityCaster caster,
        IAbilityMovement movement,
        IAbilityDebuffService debuffService,
        IAbilityStatProvider statProvider,
        IGlobalStatSource globalStatSource)
    {
        Definition = ability ?? throw new ArgumentNullException(nameof(ability));
        Caster = caster;
        Movement = movement;
        DebuffService = debuffService;
        StatProvider = statProvider ?? throw new ArgumentNullException(nameof(statProvider));
        GlobalStatSource = globalStatSource;
        ProjectileLauncher = caster != null ? caster.ProjectileLauncher : null;
        ModifierManager = caster != null ? caster.ModifierManager : null;
        behaviours = ability.Behaviours != null
            ? new List<AbilityBehaviour>(ability.Behaviours)
            : new List<AbilityBehaviour>();

        behaviourContext = new AbilityBehaviourContext(
            Definition,
            Caster,
            Movement,
            ProjectileLauncher,
            ModifierManager,
            this,
            DebuffService,
            StatProvider,
            GlobalStatSource);

        foreach (var behaviour in behaviours)
            behaviour?.Initialize(behaviourContext);
    }

    public void Cast(Vector3 worldPos, bool isHeld)
    {
        StopCastingState();
        activeCastContext = new AbilityCastContext(behaviourContext, worldPos, isHeld);
        StartCast(worldPos);
        ApplyDebuffs(Definition.DebuffsOnCast);
    }

    public void EndHold(Vector3 worldPos)
    {
        if (activeCastContext == null)
            return;

        activeCastContext.UpdateHoldState(false);

        foreach (var behaviour in behaviours)
            behaviour?.OnHoldEnded(activeCastContext);
    }

    public void Update(float deltaTime)
    {
        if (activeCastContext == null)
            return;

        if (hasPendingStartSequence)
        {
            pendingDelayRemaining -= deltaTime;
            if (pendingDelayRemaining <= 0f)
            {
                hasPendingStartSequence = false;
                RunStartBehavioursFromIndex(pendingStartIndex);
                RefreshCastingState();
            }
        }

        if (IsCasting && requiresUpdate)
        {
            foreach (var behaviour in behaviours)
            {
                if (behaviour?.RequiresUpdate == true)
                {
                    behaviour.OnCastUpdated(activeCastContext, deltaTime);
                    if (activeCastContext == null)
                        break;
                }
            }
        }

        TryAutoEnd();
    }

    public void EndCast(Vector3 worldPos, bool isSuccessful = true)
    {
        if (activeCastContext == null)
            return;

        AbilityCastContext context = activeCastContext;
        StopCastingState();
        activeCastContext = null;

        foreach (AbilityBehaviour behaviour in behaviours)
            behaviour?.OnCastEnded(context, isSuccessful);

        ApplyDebuffs(Definition.DebuffsOnEndCast);
        On_EndCast?.Invoke(isSuccessful);
    }

    public float GetStat(AbilityStatKey key)
    {
        IReadOnlyList<AbilityModifier> modifiers = ModifierManager != null ? ModifierManager.ActiveAbilityModifiers : Array.Empty<AbilityModifier>();
        IReadOnlyList<GlobalModifier> globalModifiers = ModifierManager != null ? ModifierManager.ActiveGlobalModifiers : null;
        Dictionary<GlobalStatKey, float> globalStats = GlobalStatSource != null ? GlobalStatSource.EvaluateGlobalStats() : null;
        Dictionary<AbilityStatKey, float> stats = StatProvider.EvaluateStats(Definition.TagSet, Definition.BaseStats, modifiers, globalStats);

        float value = 0f;
        if (stats != null && stats.TryGetValue(key, out float result))
            value = result;

        if (activeCastContext != null && activeCastContext.TryGetSharedStatOverride(key, out float overrideValue))
            value = overrideValue;

        return value;
    }

    public void LookAtCastDirection(Vector3 worldPos)
    {
        if (Caster == null)
            return;

        Vector3 castDirection = worldPos - Caster.transform.position;
        castDirection.y = 0f;
        if (castDirection == Vector3.zero)
            return;

        Debug.DrawRay(Caster.transform.position, castDirection, Color.yellow, 1f);
        Caster.transform.LookAt(Caster.transform.position + castDirection);
    }

    public void Disable()
    {
        StopCastingState();
        activeCastContext = null;

        foreach (var behaviour in behaviours)
            behaviour?.OnAbilityDisabled(behaviourContext);
    }

    private void StartCast(Vector3 worldPos)
    {
        On_StartCast?.Invoke(Definition);

        if (Definition.RotatingCasterToCastDirection)
            LookAtCastDirection(worldPos);

        if (Definition.StopMovementOnCast)
            Movement?.StopMovement();

        EvaluateBehaviourFlags();
        RunStartBehavioursFromIndex(0);
        RefreshCastingState();
        TryAutoEnd();
    }

    private void EvaluateBehaviourFlags()
    {
        requiresUpdate = false;
        blocksAbilityEnd = false;

        foreach (var behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            requiresUpdate |= behaviour.RequiresUpdate;
            blocksAbilityEnd |= behaviour.BlocksAbilityEnd;
        }
    }

    private void RunStartBehavioursFromIndex(int startIndex)
    {
        if (behaviours == null || activeCastContext == null)
            return;

        hasPendingStartSequence = false;
        pendingDelayRemaining = 0f;

        for (int i = startIndex; i < behaviours.Count; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            if (behaviour is AbilityDelayBehaviour delay && delay.DelaySeconds > 0f)
            {
                hasPendingStartSequence = true;
                pendingDelayRemaining = delay.DelaySeconds;
                pendingStartIndex = i + 1;
                return;
            }

            behaviour.OnCastStarted(activeCastContext);
            if (activeCastContext == null)
            {
                hasPendingStartSequence = false;
                return;
            }
        }
    }

    private void RefreshCastingState()
    {
        IsCasting = activeCastContext != null && (requiresUpdate || blocksAbilityEnd || hasPendingStartSequence);
    }

    private void TryAutoEnd()
    {
        if (activeCastContext == null)
            return;

        if (blocksAbilityEnd || requiresUpdate || hasPendingStartSequence)
            return;

        EndCast(activeCastContext.TargetPoint, true);
    }

    private void ApplyDebuffs(IReadOnlyList<ScriptableDebuff> scriptableDebuffs)
    {
        if (DebuffService == null || scriptableDebuffs == null)
            return;

        foreach (ScriptableDebuff debuff in scriptableDebuffs)
            DebuffService.AddDebuff(debuff);
    }

    private void StopCastingState()
    {
        IsCasting = false;
        hasPendingStartSequence = false;
        pendingDelayRemaining = 0f;
        pendingStartIndex = 0;
    }
}
