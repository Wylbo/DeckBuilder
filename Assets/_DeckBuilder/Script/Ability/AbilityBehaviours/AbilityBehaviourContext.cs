using UnityEngine;

public sealed class AbilityBehaviourContext
{
    public Ability Ability { get; }
    public AbilityCaster Caster { get; }
    public IAbilityMovement Movement { get; }
    public ProjectileLauncher ProjectileLauncher { get; }
    public AbilityModifierManager ModifierManager { get; }
    public IAbilityExecutor Executor { get; }
    public IAbilityDebuffService DebuffService { get; }
    public IAbilityStatProvider StatProvider { get; }

    public AbilityBehaviourContext(
        Ability ability,
        AbilityCaster caster,
        IAbilityMovement movement,
        ProjectileLauncher projectileLauncher,
        AbilityModifierManager modifierManager,
        IAbilityExecutor executor,
        IAbilityDebuffService debuffService,
        IAbilityStatProvider statProvider)
    {
        Ability = ability;
        Caster = caster;
        Movement = movement;
        ProjectileLauncher = projectileLauncher;
        ModifierManager = modifierManager;
        Executor = executor;
        DebuffService = debuffService;
        StatProvider = statProvider;
    }
}
