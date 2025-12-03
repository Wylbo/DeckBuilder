
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
    private readonly Dictionary<ScriptableDebuff, DebuffApplier> debuffs =
     new Dictionary<ScriptableDebuff, DebuffApplier>();

    public IEnumerable<DebuffApplier> ActiveDebuffs => debuffs.Values;

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
        if (debuffs.ContainsKey(debuffApplier.Debuff))
        {
            debuffs[debuffApplier.Debuff].Activate();
            debuffs[debuffApplier.Debuff].On_Ended += DebuffApplier_On_Ended;
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

        AddDebuff(scriptableDebuff.InitDebuff(this));
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
