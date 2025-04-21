using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public abstract class Ability : ScriptableObject, IHasCooldown
{
	[SerializeField]
	private bool rotatingCasterToCastDirection = true;
	[SerializeField]
	protected bool stopMovementOnCast = false;
	[SerializeField, FormerlySerializedAs("cooldown")]
	private float baseCooldown;

	[SerializeField]
	protected List<ScriptableDebuff> debuffsOnCast;
	[SerializeField]
	protected List<ScriptableDebuff> debuffsOnEndCast;
	[SerializeField] private List<AbilityTagSO> tags;

	public AbilityCaster Caster { get; private set; }
	public bool RotatingCasterToCastDirection => rotatingCasterToCastDirection;
	public float BaseCooldown => baseCooldown;
	public IReadOnlyList<AbilityTagSO> Tags => tags;

	public event UnityAction<Ability> On_StartCast;
	public event UnityAction<bool> On_EndCast;

	protected Movement movement;
	protected bool isHeld = false;


	#region IHasCooldown
	private float flatCooldownOffset;
	private float percetCooldownOffset;
	public float Cooldown => GetModifiedCooldown();

	public void AddCooldownFlatOffset(float offset)
	{
		flatCooldownOffset += offset;
	}

	public void AddCooldownPercentOffet(float percent)
	{
		percetCooldownOffset += percent;
	}

	public void ResetCooldownModifiers()
	{
		flatCooldownOffset = 0;
		percetCooldownOffset = 0;
	}

	public float GetModifiedCooldown()
	{
		float afterFlat = baseCooldown + flatCooldownOffset;
		float percent = Mathf.Clamp01(percetCooldownOffset);
		float mult = 1 - percent;
		return Mathf.Max(0f, afterFlat * mult);
	}
	#endregion

	public virtual void Initialize(AbilityCaster caster)
	{
		Caster = caster;
		movement = caster.GetComponent<Movement>();
	}

	public virtual void Disable()
	{
		Caster = null;
	}

	public void Cast(Vector3 worldPos, bool isHeld)
	{
		StartCast(worldPos);
		ApplyDebuffs(debuffsOnCast);
		this.isHeld = isHeld;
	}

	public bool HasTag(AbilityTagSO tag)
	{
		return tags.Contains(tag);
	}

	public bool HasAnyTag(params AbilityTagSO[] queryTags)
	{
		foreach (AbilityTagSO tag in queryTags)
			if (tags.Contains(tag))
				return true;
		return false;
	}

	public bool HasAllTag(params AbilityTagSO[] queryTags)
	{
		foreach (AbilityTagSO tag in queryTags)
			if (!tags.Contains(tag))
				return false;
		return true;
	}

	protected void StartCast(Vector3 worldPos)
	{
		On_StartCast?.Invoke(this);

		if (rotatingCasterToCastDirection)
			LookAtCastDirection(worldPos);

		if (stopMovementOnCast)
			movement.StopMovement();

		DoCast(worldPos);
	}

	protected virtual void DoCast(Vector3 worldPos)
	{
		EndCast(worldPos);
	}

	public virtual void EndCast(Vector3 worldPos, bool isSucessful = true)
	{
		ApplyDebuffs(debuffsOnEndCast);
		On_EndCast?.Invoke(isSucessful);
		ResetCooldownModifiers();
	}

	public virtual void EndHold(Vector3 worldPos)
	{
		isHeld = false;
	}
	protected void LookAtCastDirection(Vector3 worldPos)
	{
		Vector3 castDirection = worldPos - Caster.transform.position;
		castDirection.y = 0;
		Debug.DrawRay(Caster.transform.position, castDirection, Color.yellow, 1f);
		Caster.transform.LookAt(Caster.transform.position + castDirection);
	}

	protected void ApplyDebuffs(List<ScriptableDebuff> scriptableDebuffs)
	{
		foreach (ScriptableDebuff debuff in scriptableDebuffs)
		{
			Caster.AddDebuff(debuff);
		}
	}

}
