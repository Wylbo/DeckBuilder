using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class Ability : ScriptableObject
{
	[SerializeField]
	private bool rotatingCasterToCastDirection = true;
	[SerializeField]
	protected bool stopMovementOnCast = false;
	[SerializeField]
	private float cooldown;
	[SerializeField]
	protected List<ScriptableDebuff> debuffsOnCast;
	[SerializeField]
	protected List<ScriptableDebuff> debuffsOnEndCast;

	public AbilityCaster Caster { get; private set; }
	public bool RotatingCasterToCastDirection => rotatingCasterToCastDirection;
	public float Cooldown => cooldown;

	public event UnityAction On_StartCast;
	public event UnityAction<bool> On_EndCast;

	protected Movement movement;
	protected bool isHeld = false;

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

	protected void StartCast(Vector3 worldPos)
	{
		On_StartCast?.Invoke();

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

	protected void ApplyDebuffs(List<ScriptableDebuff> scriptableDebuffs)
	{
		foreach (ScriptableDebuff debuff in scriptableDebuffs)
		{
			Caster.AddDebuff(debuff);
		}
	}
}
