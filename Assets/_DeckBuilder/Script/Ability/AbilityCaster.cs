using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(ProjectileLauncher))]
public class AbilityCaster : MonoBehaviour
{
	[SerializeField]
	private SpellSlot[] spellSlots = new SpellSlot[4];
	[SerializeField]
	private ProjectileLauncher projectileLauncher = null;

	public ProjectileLauncher ProjectileLauncher => projectileLauncher;

	private void OnEnable()
	{
		InitializeAbilities();
	}

	private void OnDisable()
	{
		DisableAllAbilities();
	}

	private void Reset()
	{
		projectileLauncher = GetComponent<ProjectileLauncher>();
	}

	private void Update()
	{
		UpdateSpellSlotsCooldowns();
	}


	private void InitializeAbilities()
	{
		foreach (SpellSlot spellSlot in spellSlots)
		{
			spellSlot.Ability.Initialize(this);
		}
	}

	private void DisableAllAbilities()
	{
		foreach (SpellSlot spellSlot in spellSlots)
		{
			spellSlot.Ability.Disable();
		}
	}

	private void UpdateSpellSlotsCooldowns()
	{
		foreach (SpellSlot slot in spellSlots)
		{
			slot.UpdateCooldown(Time.deltaTime);
		}
	}

	public void Cast(int index, Vector3 worldPos)
	{
		Cast(spellSlots[index], worldPos);
	}

	private void Cast(SpellSlot spellSlot, Vector3 worldPos)
	{
		if (spellSlot.CanCast)
			spellSlot.Cast(this, worldPos);
	}

}
