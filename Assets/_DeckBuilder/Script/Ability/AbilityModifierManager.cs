using System;
using System.Collections.Generic;
using UnityEngine;

public class AbilityModifierManager : MonoBehaviour
{
    [SerializeField] private List<AbilityModifier> activeModifiers = new List<AbilityModifier>();
    public IReadOnlyList<AbilityModifier> ActiveModifiers => activeModifiers;

    public void Initialize()
    {
        // foreach (SpellSlot slot in caster.SpellSlots)
        // {
        //     Ability ability = slot.Ability;
        //     if (ability != null)
        //         ability.On_StartCast += ApplyModifiers;
        // }
    }

    // private void OnDisable()
    // {
    // foreach (SpellSlot slot in caster.SpellSlots)
    // {
    //     Ability ability = slot.Ability;
    //     if (ability != null)
    //         ability.On_StartCast -= ApplyModifiers;
    // }
    // }

    public void ApplyModifiers(Ability ability)
    {
        foreach (AbilityModifier mod in activeModifiers)
        {
        }
    }
}