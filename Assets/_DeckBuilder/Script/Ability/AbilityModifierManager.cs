using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class AbilityModifierManager : MonoBehaviour
{
    [SerializeField, InlineEditor] private List<AbilityModifier> activeModifiers = new List<AbilityModifier>();
    public IReadOnlyList<AbilityModifier> ActiveModifiers => activeModifiers;

}