using UnityEngine;

public class Hotbar : MonoBehaviour
{
    [SerializeField] private AbilityCaster abilityCaster;
    [SerializeField] private SpellSlotUI[] spellSlots;
    [SerializeField] private SpellSlotUI dodgeSlot;

    private void OnEnable()
    {
        BindSlots();
    }

    private void OnDisable()
    {
        ClearBindings();
    }

    public void SetCaster(AbilityCaster caster)
    {
        abilityCaster = caster;
        BindSlots();
    }

    private void BindSlots()
    {
        if (spellSlots != null)
        {
            SpellSlot[] casterSlots = abilityCaster != null ? abilityCaster.SpellSlots : null;

            for (int i = 0; i < spellSlots.Length; i++)
            {
                SpellSlot slot = casterSlots != null && i < casterSlots.Length ? casterSlots[i] : null;
                spellSlots[i]?.Bind(slot, abilityCaster, i);
            }
        }

        if (dodgeSlot != null)
        {
            SpellSlot dodge = abilityCaster != null ? abilityCaster.DodgeSpellSlot : null;
            dodgeSlot.Bind(dodge, abilityCaster, -1, true);
        }
    }

    private void ClearBindings()
    {
        if (spellSlots != null)
        {
            foreach (SpellSlotUI ui in spellSlots)
                ui?.Unbind();
        }

        dodgeSlot?.Unbind();
    }
}
