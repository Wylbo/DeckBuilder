using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
[RequireComponent(typeof(ProjectileLauncher))]
public class AbilityCaster : MonoBehaviour
{
	[SerializeField]
	private SpellSlot[] spellSlots = new SpellSlot[4];
	[SerializeField]
	private ProjectileLauncher projectileLauncher = null;
	[SerializeField]
	private DebuffUpdater debuffUpdater = null;

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

	public void AddDebuff(ScriptableDebuff scriptableDebuff)
	{
		debuffUpdater.AddDebuff(scriptableDebuff.InitDebuff(debuffUpdater));
	}

	public void RemoveDebuff(ScriptableDebuff scriptableDebuff)
	{
		debuffUpdater.RemoveDebuff(scriptableDebuff);
	}

	private void InitializeAbilities()
	{
		foreach (SpellSlot spellSlot in spellSlots)
		{
			spellSlot.Initialize(this);
		}
	}

	private void DisableAllAbilities()
	{
		foreach (SpellSlot spellSlot in spellSlots)
		{
			spellSlot.Ability?.Disable();
		}
	}

	private void UpdateSpellSlotsCooldowns()
	{
		foreach (SpellSlot slot in spellSlots)
		{
			slot.UpdateCooldown(Time.deltaTime);
		}
	}

	public bool Cast(int index, Vector3 worldPos)
	{
		return Cast(spellSlots[index], worldPos);
	}

	private bool Cast(SpellSlot spellSlot, Vector3 worldPos)
	{
		if (spellSlot.CanCast)
		{
			spellSlot.Cast(this, worldPos);
			return true;
		}

		return false;
	}

	public void EndHold(int index, Vector3 worldPos)
	{
		EndHold(spellSlots[index], worldPos);
	}

	private void EndHold(SpellSlot spellSlot, Vector3 worldPos)
	{
		spellSlot.EndHold(this, worldPos);
	}

}
