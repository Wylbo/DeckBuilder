using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class SpellSlot
{
	[SerializeField, InlineEditor]
	public Ability Ability;

        [ReadOnly]
        public Timer cooldown = new Timer();

        private bool isHeld = false;
        private AbilityCaster caster = null;
        private bool hasEndCastSubscription = false;
        private bool ownsAbilityInstance = false;
        private IAbilityExecutor executor;
        private readonly IAbilityStatProvider statProvider = new AbilityStatProvider();

	public bool HasAbility => Ability != null;
	public bool CanCast => executor != null && (cooldown == null || !cooldown.IsRunning);
        public IAbilityExecutor Executor => executor;

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
                        UnityEngine.Object.Destroy(ability);
                ownsAbilityInstance = Ability != null;
                hasEndCastSubscription = false;
                this.caster = caster;
                ResetCooldown();

                if (Ability != null && caster != null)
                {
                        var movement = caster.GetComponent<IAbilityMovement>();
                        if (movement == null)
                                movement = caster.GetComponent<Movement>();

                        IAbilityDebuffService debuffService = caster.DebuffService;
                        if (debuffService == null)
                                debuffService = caster.GetComponent<IAbilityDebuffService>();
                        IGlobalStatSource globalStats = caster.GlobalStatSource;
                        if (globalStats == null)
                                globalStats = caster.GetComponent<IGlobalStatSource>();
                        executor = new AbilityExecutor(Ability, caster, movement, debuffService, statProvider, globalStats);
                        return true;
                }

                return false;
	}

	public void Cast(AbilityCaster caster, Vector3 worldPos)
	{
		if (executor == null)
			return;

                this.caster = caster;

                isHeld = true;
                UnsubscribeFromEndCast();
                SubscribeToEndCast();
                executor.Cast(worldPos, isHeld);

                if (Ability != null && Ability.StartCooldownOnCast)
                        StartCooldown();
        }

	public void EndHold(AbilityCaster caster, Vector3 worldPos)
	{
		if (executor == null)
			return;

		this.caster = caster;
		isHeld = false;
		executor.EndHold(worldPos);
	}

	public void UpdateCooldown(float dt)
	{
		if (cooldown == null)
			cooldown = new Timer();

		cooldown.Update(dt);
                executor?.Update(dt);
	}

        public void Disable()
        {
                UnsubscribeFromEndCast();
                executor?.Disable();
	}

        private void ResetCooldown()
        {
                UnsubscribeFromEndCast();

                cooldown = new Timer();
        }

        private void Ability_OnEndCast(bool isSucessful)
        {
                UnsubscribeFromEndCast();

                if (!isSucessful || Ability == null)
                        return;

		if (!Ability.StartCooldownOnCast)
			StartCooldown();
	}

        private void StartCooldown()
        {
                float duration = executor != null ? executor.Cooldown : 0f;
                cooldown = new Timer(duration);
                cooldown.Start();
        }

        private void SubscribeToEndCast()
        {
                if (executor == null || hasEndCastSubscription)
                        return;

                executor.On_EndCast += Ability_OnEndCast;
                hasEndCastSubscription = true;
        }

        private void UnsubscribeFromEndCast()
        {
                if (executor == null || !hasEndCastSubscription)
                        return;

                executor.On_EndCast -= Ability_OnEndCast;
                hasEndCastSubscription = false;
        }

        private void CleanupAbilityInstance(bool destroyAbilityInstance = true)
        {
                UnsubscribeFromEndCast();

                executor?.Disable();
                executor = null;

                if (destroyAbilityInstance && Ability != null && ownsAbilityInstance)
                {
                        UnityEngine.Object.Destroy(Ability);
                }

                ownsAbilityInstance = false;
        }
}
