using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IAbilityModifierProvider
{
    IReadOnlyList<AbilityModifier> ActiveAbilityModifiers { get; }
}

public interface IGlobalModifierProvider
{
    IReadOnlyList<GlobalModifier> ActiveGlobalModifiers { get; }
}

public class AbilityModifierManager : MonoBehaviour, IAbilityModifierProvider, IGlobalModifierProvider
{
    [SerializeField, InlineEditor] private List<AbilityModifier> activeAbilityModifiers = new List<AbilityModifier>();
    [SerializeField, InlineEditor] private List<GlobalModifier> activeGlobalModifiers = new List<GlobalModifier>();

    // Legacy alias
    public IReadOnlyList<AbilityModifier> ActiveModifiers => activeAbilityModifiers;

    public IReadOnlyList<AbilityModifier> ActiveAbilityModifiers => activeAbilityModifiers;
    public IReadOnlyList<GlobalModifier> ActiveGlobalModifiers => activeGlobalModifiers;

}
