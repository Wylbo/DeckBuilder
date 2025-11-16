using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = nameof(Ability), menuName = FileName.AbilityFolder + nameof(Ability), order = 0)]
public class Ability : ScriptableObject
{
	[SerializeField] private bool rotatingCasterToCastDirection = true;
	[SerializeField] protected bool stopMovementOnCast = false;
	[SerializeField] protected List<ScriptableDebuff> debuffsOnCast;
	[SerializeField] protected List<ScriptableDebuff> debuffsOnEndCast;
	[SerializeField] private GTagSet tagSet = new GTagSet();
	[SerializeField] private List<AbilityStatEntry> baseStats;
	[SerializeReference]
	private List<AbilityBehaviour> behaviours = new List<AbilityBehaviour>();

	// [SerializeField, InlineEditor] private AbilitySharedStats sharedStats;

	public AbilityCaster Caster { get; private set; }
	public bool RotatingCasterToCastDirection => rotatingCasterToCastDirection;
	public GTagSet TagSet => tagSet;
	public IReadOnlyList<AbilityStatEntry> BaseStats => baseStats;
	public IReadOnlyList<ScriptableDebuff> DebuffsOnCast => debuffsOnCast;

	public event UnityAction<Ability> On_StartCast;
	public event UnityAction<bool> On_EndCast;

	protected Movement movement;
	protected bool isHeld = false;
	private AbilityBehaviourContext behaviourContext;
	private AbilityCastContext activeCastContext;
	private Coroutine castRoutine;
	private bool isCasting;

	public float Cooldown
	{
		get
		{
			var stats = EvaluateStats(Caster.ModifierManager.ActiveModifiers);
			return StatOr(stats, AbilityStatKey.Cooldown, GetBaseStatValue(AbilityStatKey.Cooldown));
		}
	}

	public virtual void Initialize(AbilityCaster caster)
	{
		Caster = caster;
		movement = caster.GetComponent<Movement>();
		behaviourContext = new AbilityBehaviourContext(this, caster, movement);
		foreach (var behaviour in behaviours)
			behaviour?.Initialize(behaviourContext);
	}

	public virtual void Disable()
	{
		StopCastRoutine();
		if (behaviourContext != null)
		{
			foreach (var behaviour in behaviours)
				behaviour?.OnAbilityDisabled(behaviourContext);
		}
		activeCastContext = null;
		isCasting = false;
		behaviourContext = null;
		Caster = null;
	}

	public void Cast(Vector3 worldPos, bool isHeld)
	{
		activeCastContext = new AbilityCastContext(behaviourContext, worldPos, isHeld);
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
		if (activeCastContext == null)
			activeCastContext = new AbilityCastContext(behaviourContext, worldPos, isHeld);

		bool requiresUpdate = false;
		bool blocksAutoEnd = false;

		foreach (var behaviour in behaviours)
		{
			if (behaviour == null)
				continue;

			behaviour.OnCastStarted(activeCastContext);
			requiresUpdate |= behaviour.RequiresUpdate;
			blocksAutoEnd |= behaviour.BlocksAbilityEnd;
		}

		isCasting = requiresUpdate || blocksAutoEnd;

		if (requiresUpdate && Caster != null)
		{
			castRoutine = Caster.StartCoroutine(CastUpdateRoutine());
		}

		if (!isCasting)
			EndCast(worldPos);
	}

	public virtual void EndCast(Vector3 worldPos, bool isSucessful = true)
	{
		if (activeCastContext == null)
			return;

		StopCastRoutine();
		var context = activeCastContext;
		activeCastContext = null;
		isCasting = false;

		foreach (var behaviour in behaviours)
			behaviour?.OnCastEnded(context, isSucessful);

		ApplyDebuffs(debuffsOnEndCast);
		On_EndCast?.Invoke(isSucessful);
	}

	public virtual void EndHold(Vector3 worldPos)
	{
		isHeld = false;
		if (activeCastContext == null)
			return;

		activeCastContext.UpdateHoldState(false);
		foreach (var behaviour in behaviours)
			behaviour?.OnHoldEnded(activeCastContext);
	}
	public void LookAtCastDirection(Vector3 worldPos)
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
		if (scriptableDebuffs == null)
			return;

		foreach (ScriptableDebuff debuff in scriptableDebuffs)
		{
			Caster.AddDebuff(debuff);
		}
	}

	protected void RemoveDebuffs(List<ScriptableDebuff> scriptableDebuffs)
	{
		if (scriptableDebuffs == null)
			return;

		foreach (ScriptableDebuff debuff in scriptableDebuffs)
		{
			Caster.RemoveDebuff(debuff);
		}
	}
	#endregion

	#region Modifier Application
	protected virtual IEnumerable<AbilityStatEntry> GetBaseStats()
	{
		var merged = new List<AbilityStatEntry>();
		merged.AddRange(baseStats);
		// if (sharedStats != null)
		// 	merged.AddRange(sharedStats.Stats);

		foreach (var stat in merged)
			yield return stat;
	}

	protected float GetBaseStatValue(AbilityStatKey key)
	{
		foreach (var stat in GetBaseStats())
		{
			if (stat.Key == key)
				return stat.Value;
		}
		return 0f;
	}

	protected Dictionary<AbilityStatKey, float> EvaluateStats(IEnumerable<AbilityModifier> activeModifiers)
	{
		return AbilityModifierRuntime.Evaluate(TagSet, GetBaseStats(), activeModifiers);
	}

	public float GetEvaluatedStatValue(AbilityStatKey key)
	{
		var stats = EvaluateStats(Caster.ModifierManager.ActiveModifiers);
		return StatOr(stats, key, GetBaseStatValue(key));
	}

	protected static float StatOr(Dictionary<AbilityStatKey, float> dict, AbilityStatKey key, float defVal)
	{
		return dict != null && dict.TryGetValue(key, out float val) ? val : defVal;
	}
	#endregion

	private IEnumerator CastUpdateRoutine()
	{
		while (isCasting && activeCastContext != null)
		{
			float deltaTime = Time.deltaTime;
			foreach (var behaviour in behaviours)
			{
				if (behaviour?.RequiresUpdate == true)
					behaviour.OnCastUpdated(activeCastContext, deltaTime);
			}

			yield return null;
		}
	}

	private void StopCastRoutine()
	{
		if (castRoutine != null && Caster != null)
		{
			Caster.StopCoroutine(castRoutine);
			castRoutine = null;
		}
	}

}
