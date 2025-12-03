
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum DebuffStackingPolicy
{
    Refresh,
    Single,
    Stack,
}
public enum DebuffDurationPolicy
{
    Additive,
    Refresh,
    none,
}
public class DebuffUpdater : MonoBehaviour, IAbilityDebuffService
{
    [SerializeField] private StatsModifierManager modifierManager;
    private readonly Dictionary<ScriptableDebuff, DebuffApplier> debuffs =
     new Dictionary<ScriptableDebuff, DebuffApplier>();

    public IEnumerable<DebuffApplier> ActiveDebuffs => debuffs.Values;
    public StatsModifierManager ModifierManager => modifierManager;

    private void Awake()
    {
        if (modifierManager == null)
            modifierManager = GetComponent<StatsModifierManager>();
    }

    #region Unity messages
    private void Update()
    {
        foreach (DebuffApplier debuff in debuffs.Values.ToList())
        {
            debuff.Tick(Time.deltaTime);
        }
    }
    #endregion

    #region DebuffUpdater
    public void AddDebuff(DebuffApplier debuffApplier)
    {
        if (debuffApplier == null || debuffApplier.Debuff == null)
            return;

        if (debuffs.ContainsKey(debuffApplier.Debuff))
        {
            DebuffApplier existingApplier = debuffs[debuffApplier.Debuff];
            existingApplier.On_Ended -= DebuffApplier_On_Ended;
            existingApplier.On_Ended += DebuffApplier_On_Ended;
            existingApplier.Activate();
        }
        else
        {
            debuffs.Add(debuffApplier.Debuff, debuffApplier);
            debuffApplier.Activate();
            debuffApplier.On_Ended += DebuffApplier_On_Ended;
        }
    }

    public void AddDebuff(ScriptableDebuff scriptableDebuff)
    {
        if (scriptableDebuff == null)
            return;

        AddDebuff(new DebuffApplier(scriptableDebuff, this));
    }

    public void RemoveDebuff(ScriptableDebuff scriptableDebuff)
    {
        if (debuffs.ContainsKey(scriptableDebuff))
        {
            debuffs[scriptableDebuff].Remove();
        }
    }

    private void DebuffApplier_On_Ended(DebuffApplier applier)
    {
        applier.On_Ended -= DebuffApplier_On_Ended;
        debuffs.Remove(applier.Debuff);
    }
    #endregion
}
