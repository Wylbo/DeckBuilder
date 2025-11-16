using System;

[Serializable]
public abstract class AbilityBehaviour
{
	public virtual void Initialize(AbilityBehaviourContext context) { }

	public virtual void OnAbilityDisabled(AbilityBehaviourContext context) { }

	public virtual bool RequiresUpdate => false;

	public virtual bool BlocksAbilityEnd => false;

	public virtual void OnCastStarted(AbilityCastContext context) { }

	public virtual void OnCastUpdated(AbilityCastContext context, float deltaTime) { }

	public virtual void OnCastEnded(AbilityCastContext context, bool wasSuccessful) { }

	public virtual void OnHoldEnded(AbilityCastContext context) { }
}
