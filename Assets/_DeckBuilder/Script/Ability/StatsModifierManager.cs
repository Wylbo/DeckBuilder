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

public class StatsModifierManager : MonoBehaviour, IAbilityModifierProvider, IGlobalModifierProvider
{
    [SerializeField, InlineEditor] private ObservableList<AbilityModifier> activeAbilityModifiers = new ObservableList<AbilityModifier>();
    [SerializeField, InlineEditor] private ObservableList<GlobalModifier> activeGlobalModifiers = new ObservableList<GlobalModifier>();

    public IReadOnlyList<AbilityModifier> ActiveAbilityModifiers => activeAbilityModifiers;
    public IReadOnlyList<GlobalModifier> ActiveGlobalModifiers => activeGlobalModifiers;

    public event Action OnAbilityModifiersChanged;
    public event Action OnGlobalModifiersChanged;
    public event Action OnAnyModifiersChanged;

    private void OnEnable()
    {
        activeAbilityModifiers.OnItemAdded += (_) => NotifyAbilityModifiersChanged();
        activeAbilityModifiers.OnItemRemoved += (_) => NotifyAbilityModifiersChanged();

        activeGlobalModifiers.OnItemAdded += (_) => NotifyGlobalModifiersChanged();
        activeGlobalModifiers.OnItemRemoved += (_) => NotifyGlobalModifiersChanged();
    }

    void OnDisable()
    {
        activeAbilityModifiers.OnItemAdded -= (_) => NotifyAbilityModifiersChanged();
        activeAbilityModifiers.OnItemRemoved -= (_) => NotifyAbilityModifiersChanged();

        activeGlobalModifiers.OnItemAdded -= (_) => NotifyGlobalModifiersChanged();
        activeGlobalModifiers.OnItemRemoved -= (_) => NotifyGlobalModifiersChanged();
    }

    public void AddAbilityModifier(AbilityModifier modifier)
    {
        if (modifier == null) return;
        activeAbilityModifiers.Add(modifier);
        NotifyAbilityModifiersChanged();
    }

    public bool RemoveAbilityModifier(AbilityModifier modifier)
    {
        bool removed = activeAbilityModifiers.Remove(modifier);
        if (removed) NotifyAbilityModifiersChanged();
        return removed;
    }

    public void AddGlobalModifier(GlobalModifier modifier)
    {
        if (modifier == null) return;
        activeGlobalModifiers.Add(modifier);
        NotifyGlobalModifiersChanged();
    }

    public bool RemoveGlobalModifier(GlobalModifier modifier)
    {
        bool removed = activeGlobalModifiers.Remove(modifier);
        if (removed) NotifyGlobalModifiersChanged();
        return removed;
    }

    public void NotifyAbilityModifiersChanged()
    {
        OnAbilityModifiersChanged?.Invoke();
        OnAnyModifiersChanged?.Invoke();
    }

    public void NotifyGlobalModifiersChanged()
    {
        OnGlobalModifiersChanged?.Invoke();
        OnAnyModifiersChanged?.Invoke();
    }

    public void NotifyAnyModifiersChanged()
    {
        OnAnyModifiersChanged?.Invoke();
    }
}
