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

	public bool HasAbility => Ability != null;
	public bool CanCast => HasAbility && (cooldown == null || !cooldown.IsRunning);

	public bool Initialize(AbilityCaster caster)
	{
		return SetAbility(Ability, caster);
	}

        public bool SetAbility(Ability ability, AbilityCaster caster)
        {
                // Clean up existing ability instance
                if (Ability != null)
                {
                        UnsubscribeFromEndCast();

                        bool shouldDestroyInstance = Ability.Caster != null;
                        Ability.Disable();

                        if (shouldDestroyInstance)
                        {
                                // Prevent ability reassignment from accumulating runtime instances
                                UnityEngine.Object.Destroy(Ability);
                        }
                }

                Ability = ability != null ? UnityEngine.Object.Instantiate(ability) : null;
                hasEndCastSubscription = false;
                this.caster = caster;
                ResetCooldown();

		if (Ability != null && caster != null)
		{
			Ability.Initialize(caster);
			return true;
		}

		return false;
	}

	public void Cast(AbilityCaster caster, Vector3 worldPos)
	{
		if (Ability == null)
			return;

                this.caster = caster;

                isHeld = true;
                UnsubscribeFromEndCast();
                SubscribeToEndCast();
                Ability.Cast(worldPos, isHeld);

                if (Ability.StartCooldownOnCast)
                        StartCooldown();
        }

	public void EndHold(AbilityCaster caster, Vector3 worldPos)
	{
		if (Ability == null)
			return;

		this.caster = caster;
		isHeld = false;
		Ability.EndHold(worldPos);
	}

	public void UpdateCooldown(float dt)
	{
		if (cooldown == null)
			cooldown = new Timer();

		cooldown.Update(dt);
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
                cooldown = new Timer(Ability.Cooldown);
                cooldown.Start();
        }

        private void SubscribeToEndCast()
        {
                if (Ability == null || hasEndCastSubscription)
                        return;

                Ability.On_EndCast += Ability_OnEndCast;
                hasEndCastSubscription = true;
        }

        private void UnsubscribeFromEndCast()
        {
                if (Ability == null || !hasEndCastSubscription)
                        return;

                Ability.On_EndCast -= Ability_OnEndCast;
                hasEndCastSubscription = false;
        }
}
