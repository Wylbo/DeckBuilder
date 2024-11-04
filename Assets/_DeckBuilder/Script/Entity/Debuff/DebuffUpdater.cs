
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
public class DebuffUpdater : MonoBehaviour
{
    private readonly Dictionary<ScriptableDebuff, DebuffApplier> debuffs =
     new Dictionary<ScriptableDebuff, DebuffApplier>();

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

    private void DebuffApplier_On_Ended(DebuffApplier applier)
    {
        applier.On_Ended -= DebuffApplier_On_Ended;
        debuffs.Remove(applier.Debuff);
    }
    #endregion
}