using UnityEngine;

public sealed class AbilityBehaviourContext
{
    public Ability Ability { get; }
    public AbilityCaster Caster { get; }
    public Movement Movement { get; }

    public ProjectileLauncher ProjectileLauncher => Caster.ProjectileLauncher;
    public AbilityModifierManager ModifierManager => Caster.ModifierManager;

    public AbilityBehaviourContext(Ability ability, AbilityCaster caster, Movement movement)
    {
        Ability = ability;
        Caster = caster;
        Movement = movement;
    }
}
