
using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebuffStackingPolicy
{
    Single,
    Stack,
}
public class DebuffUpdater : MonoBehaviour
{
    [SerializeField]
    private List<Debuff> debuffs;

    private List<Debuff> stackingDebuffs;
    private List<Debuff> singlesDebuffs;

    #region Unity messages
    private void Update()
    {
        if (debuffs != null)
        {
            UpdateDebuffs();
        }
    }
    #endregion

    #region DebuffUpdater
    public bool AddDebuff(Debuff debuff)
    {
        return debuff.StackingPolicy switch
        {
            DebuffStackingPolicy.Single => TryAddSingleDebuff(debuff),
            DebuffStackingPolicy.Stack => TryAddStackingDebuff(debuff),
            _ => false,
        };
    }

    private bool TryAddSingleDebuff(Debuff debuff)
    {
        if (singlesDebuffs.Contains(debuff))
            return false;
        else
        {
            singlesDebuffs.Add(debuff);
            return true;
        }
    }

    private bool TryAddStackingDebuff(Debuff debuff)
    {
        stackingDebuffs.Add(debuff);
        return true;
    }


    private void UpdateDebuffs()
    {
        foreach (Debuff debuff in debuffs)
        {
            debuff.Update();
        }
    }
    #endregion
}