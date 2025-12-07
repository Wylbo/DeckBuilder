using UnityEngine;

public sealed class AbilityBehaviourContext
{
    public Ability Ability { get; }
    public AbilityCaster Caster { get; }
    public IAbilityMovement Movement { get; }
    public AnimationHandler AnimationHandler { get; }
    public ProjectileLauncher ProjectileLauncher { get; }
    public StatsModifierManager ModifierManager { get; }
    public IAbilityExecutor Executor { get; }
    public IAbilityDebuffService DebuffService { get; }
    public IAbilityStatProvider StatProvider { get; }
    public IGlobalStatSource GlobalStatSource { get; }

    public AbilityBehaviourContext(
        Ability ability,
        AbilityCaster caster,
        IAbilityMovement movement,
        AnimationHandler animationHandler,
        ProjectileLauncher projectileLauncher,
        StatsModifierManager modifierManager,
        IAbilityExecutor executor,
        IAbilityDebuffService debuffService,
        IAbilityStatProvider statProvider,
        IGlobalStatSource globalStatSource)
    {
        Ability = ability;
        Caster = caster;
        Movement = movement;
        AnimationHandler = animationHandler;
        ProjectileLauncher = projectileLauncher;
        ModifierManager = modifierManager;
        Executor = executor;
        DebuffService = debuffService;
        StatProvider = statProvider;
        GlobalStatSource = globalStatSource;
    }
}
