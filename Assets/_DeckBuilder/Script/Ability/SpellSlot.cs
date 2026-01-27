using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class SpellSlot
{
    #region Fields
    [SerializeField, InlineEditor] public Ability Ability;
    [ReadOnly] public Timer cooldown = new Timer();
    public event Action<SpellSlot, float> OnCooldownStarted;
    public event Action<SpellSlot> OnCooldownEnded;
    public event Action<SpellSlot, bool> OnCastStateChanged;
    public event Action<SpellSlot, Vector3> OnCastStarted;
    public event Action<SpellSlot> OnAbilityChanged;
    #endregion

    #region Private Members
    private bool isHeld = false;
    private AbilityCaster caster = null;
    private bool hasEndCastSubscription = false;
    private bool ownsAbilityInstance = false;
    private IAbilityExecutor executor;
    private readonly IAbilityStatProvider statProvider = new AbilityStatProvider();
    #endregion

    #region Getters
    public bool HasAbility => Ability != null;
    public bool CanCast => executor != null && (cooldown == null || !cooldown.IsRunning);
    public IAbilityExecutor Executor => executor;
    #endregion

    #region Unity Message Methods
    #endregion

    #region Public Methods
    public bool Initialize(AbilityCaster caster)
    {
        return SetAbility(Ability, caster);
    }

    public bool SetAbility(Ability ability, AbilityCaster caster)
    {
        bool reusingOwnedAbilityInstance = ownsAbilityInstance && ReferenceEquals(Ability, ability);

        CleanupAbilityInstance(!reusingOwnedAbilityInstance);

        Ability = ability != null ? UnityEngine.Object.Instantiate(ability) : null;
        if (reusingOwnedAbilityInstance && ability != null)
        {
            UnityEngine.Object.Destroy(ability);
        }

        ownsAbilityInstance = Ability != null;
        hasEndCastSubscription = false;
        this.caster = caster;
        ResetCooldown();

        if (Ability != null && caster != null)
        {
            IAbilityMovement movement = caster.GetComponent<IAbilityMovement>();
            if (movement == null)
            {
                movement = caster.GetComponent<Movement>();
            }

            AnimationHandler animationHandler = caster.GetComponent<AnimationHandler>();

            IAbilityDebuffService debuffService = caster.DebuffService;
            if (debuffService == null)
            {
                debuffService = caster.GetComponent<IAbilityDebuffService>();
            }

            IGlobalStatSource globalStats = caster.GlobalStatSource;
            if (globalStats == null)
            {
                globalStats = caster.GetComponent<IGlobalStatSource>();
            }

            executor = new AbilityExecutor(Ability, caster, movement, animationHandler, debuffService, statProvider, globalStats);
            OnAbilityChanged?.Invoke(this);
            return true;
        }

        OnAbilityChanged?.Invoke(this);
        return false;
    }

    public void Cast(AbilityCaster caster, Vector3 targetPoint, Vector3 aimPoint, bool isHeldRequest)
    {
        if (executor == null)
        {
            return;
        }

        this.caster = caster;
        isHeld = isHeldRequest;

        UnsubscribeFromEndCast();
        SubscribeToEndCast();
        executor.Cast(targetPoint, aimPoint, isHeld);
        OnCastStarted?.Invoke(this, targetPoint);
        OnCastStateChanged?.Invoke(this, true);

        if (Ability != null && Ability.StartCooldownOnCast)
        {
            StartCooldown();
        }
    }

    public void EndHold(AbilityCaster caster, Vector3 worldPos)
    {
        if (executor == null)
        {
            return;
        }

        this.caster = caster;
        isHeld = false;
        executor.EndHold(worldPos);
    }

    public void UpdateCooldown(float dt)
    {
        if (cooldown == null)
        {
            cooldown = new Timer();
        }

        cooldown.Update(dt);
        executor?.Update(dt);
    }

    public void Disable()
    {
        UnsubscribeFromEndCast();
        executor?.Disable();
        OnCastStateChanged?.Invoke(this, false);
    }
    #endregion

    #region Private Methods
    private void ResetCooldown()
    {
        UnsubscribeFromEndCast();

        DetachCooldownCallbacks();
        cooldown = new Timer();
        OnCooldownEnded?.Invoke(this);
    }

    private void Ability_OnEndCast(bool isSucessful)
    {
        UnsubscribeFromEndCast();

        if (!isSucessful || Ability == null)
        {
            return;
        }

        if (!Ability.StartCooldownOnCast)
        {
            StartCooldown();
        }

        OnCastStateChanged?.Invoke(this, false);
    }

    private void StartCooldown()
    {
        float duration = executor != null ? executor.Cooldown : 0f;
        DetachCooldownCallbacks();
        cooldown = new Timer(duration);
        cooldown.On_Ended += HandleCooldownEnded;
        cooldown.Start();
        OnCooldownStarted?.Invoke(this, duration);
    }

    private void HandleCooldownEnded()
    {
        OnCooldownEnded?.Invoke(this);
        cooldown.On_Ended -= HandleCooldownEnded;
    }

    private void SubscribeToEndCast()
    {
        if (executor == null || hasEndCastSubscription)
        {
            return;
        }

        executor.On_EndCast += Ability_OnEndCast;
        hasEndCastSubscription = true;
    }

    private void UnsubscribeFromEndCast()
    {
        if (executor == null || !hasEndCastSubscription)
        {
            return;
        }

        executor.On_EndCast -= Ability_OnEndCast;
        hasEndCastSubscription = false;
    }

    private void CleanupAbilityInstance(bool destroyAbilityInstance = true)
    {
        UnsubscribeFromEndCast();

        executor?.Disable();
        executor = null;
        DetachCooldownCallbacks();

        if (destroyAbilityInstance && Ability != null && ownsAbilityInstance)
        {
            UnityEngine.Object.Destroy(Ability);
        }

        ownsAbilityInstance = false;
        OnCastStateChanged?.Invoke(this, false);
        OnCooldownEnded?.Invoke(this);
    }

    private void DetachCooldownCallbacks()
    {
        if (cooldown != null)
        {
            cooldown.On_Ended -= HandleCooldownEnded;
        }
    }
    #endregion
}
