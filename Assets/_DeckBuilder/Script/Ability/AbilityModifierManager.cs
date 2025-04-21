using System;
using System.Collections.Generic;
using UnityEngine;

public class AbilityModifierManager : MonoBehaviour
{
    [SerializeField] private List<AbilityModifier> allModifiers = new List<AbilityModifier>();

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
        foreach (AbilityModifier mod in allModifiers)
        {
            if (!mod.AppliesTo(ability))
                continue;

            mod.Apply(ability);
        }
    }
}