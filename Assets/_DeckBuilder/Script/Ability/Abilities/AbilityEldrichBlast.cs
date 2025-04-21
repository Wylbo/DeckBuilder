using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityEldrichBlast), menuName = FileName.Abilities + nameof(AbilityEldrichBlast), order = 0)]
public class AbilityEldrichBlast : AbilityTargeted
{
    [SerializeField] private int damage;
    protected override void DoTargetAbilityAtCursorPos(Vector3 worldPos)
    {
        throw new System.NotImplementedException();
    }

    protected override void DoTargetedAbility(Targetable target)
    {
        target.Character.TakeDamage(damage);
    }
}