using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public abstract class Ability : ScriptableObject
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
	[SerializeField] private List<AbilityStatEntry> baseStats;

	public AbilityCaster Caster { get; private set; }
	public bool RotatingCasterToCastDirection => rotatingCasterToCastDirection;
	public float BaseCooldown => baseCooldown;
	public IReadOnlyList<AbilityTagSO> Tags => tags;
	public IReadOnlyList<AbilityStatEntry> BaseStats => baseStats;

	public event UnityAction<Ability> On_StartCast;
	public event UnityAction<bool> On_EndCast;

	protected Movement movement;
	protected bool isHeld = false;

	public float Cooldown
	{
		get
		{
			var stats = EvaluateStats(Caster.ModifierManager.ActiveModifiers);
			return StatOr(stats, AbilityStatKey.Cooldown, baseCooldown);
		}
	}


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

	#region  Casting
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
	#endregion

	#region Buff Application
	protected void ApplyDebuffs(List<ScriptableDebuff> scriptableDebuffs)
	{
		foreach (ScriptableDebuff debuff in scriptableDebuffs)
		{
			Caster.AddDebuff(debuff);
		}
	}
	#endregion

	#region Modifier Application
	protected virtual IEnumerable<AbilityStatEntry> GetBaseStats()
	{
		yield return new AbilityStatEntry
		{
			Key = AbilityStatKey.Cooldown,
			Value = baseCooldown
		};
	}
	protected Dictionary<AbilityStatKey, float> EvaluateStats(IEnumerable<AbilityModifier> activeModifiers)
	{
		return AbilityModifierRuntime.Evaluate(Tags, GetBaseStats(), activeModifiers);
	}

	protected static float StatOr(Dictionary<AbilityStatKey, float> dict, AbilityStatKey key, float defVal)
	{
		return dict != null && dict.TryGetValue(key, out float val) ? val : defVal;
	}
	#endregion

}
