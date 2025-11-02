using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

[System.Serializable]
public class IsAlive : ConditionNode
{
    Character character;
    protected override void OnStart()
    {
        base.OnStart();
        character = context.GetComponent<Character>();
    }

    protected override bool CheckCondition()
    {
        return character != null && character.Health.Value > 0;
    }

}