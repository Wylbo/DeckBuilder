using System;
using UnityEngine;

/// <summary>
/// Handles ability cast presentation on the client side.
/// Responsible for applying rotation and playing animations when cast events are received.
/// </summary>
public sealed class AbilityCastPresenter
{
    #region Fields
    private readonly AnimationHandler animationHandler;
    private readonly AbilityCaster abilityCaster;

    /// <summary>Raised when a cast starts. Parameters: slotIndex, isDodgeSlot, worldPos, abilityId.</summary>
    public event Action<int, bool, Vector3, string> OnCastStarted;

    /// <summary>Raised when a cast ends. Parameters: slotIndex, isDodgeSlot.</summary>
    public event Action<int, bool> OnCastEnded;
    #endregion

    #region Private Members
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    #endregion

    #region Public Methods
    /// <summary>
    /// Initializes a new instance of the AbilityCastPresenter.
    /// </summary>
    /// <param name="animationHandler">The animation handler for playing ability animations.</param>
    /// <param name="abilityCaster">The ability caster to apply rotations to.</param>
    public AbilityCastPresenter(AnimationHandler animationHandler, AbilityCaster abilityCaster)
    {
        this.animationHandler = animationHandler;
        this.abilityCaster = abilityCaster;
    }

    /// <summary>
    /// Handles the cast started event for clients.
    /// Applies rotation and plays animation to match server state.
    /// </summary>
    /// <param name="slotIndex">The spell slot index.</param>
    /// <param name="isDodgeSlot">Whether the dodge slot was used.</param>
    /// <param name="worldPos">The world position targeted by the cast.</param>
    /// <param name="abilityId">The ability identifier.</param>
    public void HandleCastStarted(int slotIndex, bool isDodgeSlot, Vector3 worldPos, string abilityId)
    {
        OnCastStarted?.Invoke(slotIndex, isDodgeSlot, worldPos, abilityId);

        Ability ability = ResolveAbility(slotIndex, isDodgeSlot);
        if (ability == null)
        {
            return;
        }

        if (ability.RotatingCasterToCastDirection)
        {
            ApplyCastRotation(worldPos);
        }

        if (ability.AnimationData != null && animationHandler != null)
        {
            animationHandler.PlayAnimation(ability.AnimationData);
        }
    }

    /// <summary>
    /// Handles the cast ended event for clients.
    /// Stops the ability animation.
    /// </summary>
    /// <param name="slotIndex">The spell slot index.</param>
    /// <param name="isDodgeSlot">Whether the dodge slot was used.</param>
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
    /// <summary>
    /// Applies rotation to the caster to face the cast direction.
    /// </summary>
    /// <param name="worldPos">The world position to face.</param>
    private void ApplyCastRotation(Vector3 worldPos)
    {
        if (abilityCaster == null)
        {
            return;
        }

        Transform casterTransform = abilityCaster.transform;
        Vector3 castDirection = worldPos - casterTransform.position;
        castDirection.y = 0f;

        if (castDirection == Vector3.zero)
        {
            return;
        }

        casterTransform.LookAt(casterTransform.position + castDirection);
    }

    /// <summary>
    /// Resolves the ability from the given slot.
    /// </summary>
    /// <param name="slotIndex">The spell slot index.</param>
    /// <param name="isDodgeSlot">Whether to resolve from the dodge slot.</param>
    /// <returns>The ability if found; otherwise null.</returns>
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

    /// <summary>
    /// Resolves the spell slot from the caster by index.
    /// </summary>
    /// <param name="slotIndex">The spell slot index.</param>
    /// <returns>The spell slot if found; otherwise null.</returns>
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
