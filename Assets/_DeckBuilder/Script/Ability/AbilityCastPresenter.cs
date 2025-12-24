using System;
using UnityEngine;

public sealed class AbilityCastPresenter
{
    #region Fields
    private readonly AnimationHandler animationHandler;
    private readonly AbilityCaster abilityCaster;
    public event Action<int, bool, Vector3, string> OnCastStarted;
    public event Action<int, bool> OnCastEnded;
    #endregion

    #region Private Members
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    #endregion

    #region Public Methods
    public AbilityCastPresenter(AnimationHandler animationHandler, AbilityCaster abilityCaster)
    {
        this.animationHandler = animationHandler;
        this.abilityCaster = abilityCaster;
    }

    public void HandleCastStarted(int slotIndex, bool isDodgeSlot, Vector3 worldPos, string abilityId)
    {
        OnCastStarted?.Invoke(slotIndex, isDodgeSlot, worldPos, abilityId);

        Ability ability = ResolveAbility(slotIndex, isDodgeSlot);
        if (ability != null && ability.AnimationData != null && animationHandler != null)
        {
            animationHandler.PlayAnimation(ability.AnimationData);
        }
    }

    public void HandleCastEnded(int slotIndex, bool isDodgeSlot)
    {
        OnCastEnded?.Invoke(slotIndex, isDodgeSlot);

        Ability ability = ResolveAbility(slotIndex, isDodgeSlot);
        if (ability != null && ability.AnimationData != null && animationHandler != null)
        {
            animationHandler.StopAnimation(ability.AnimationData);
        }
    }
    #endregion

    #region Private Methods
    private Ability ResolveAbility(int slotIndex, bool isDodgeSlot)
    {
        if (abilityCaster == null)
        {
            return null;
        }

        SpellSlot slot = isDodgeSlot ? abilityCaster.DodgeSpellSlot : ResolveSpellSlot(slotIndex);
        if (slot != null && slot.Ability != null)
        {
            return slot.Ability;
        }

        return null;
    }

    private SpellSlot ResolveSpellSlot(int slotIndex)
    {
        SpellSlot[] slots = abilityCaster.SpellSlots;
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length)
        {
            return null;
        }

        return slots[slotIndex];
    }
    #endregion
}
