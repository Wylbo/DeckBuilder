using UnityEngine;

[CreateAssetMenu(fileName = "DebuffSlow", menuName = FileName.Debuff + "DebuffSlow", order = 0)]
public class DebuffMovementSpeed : Debuff
{
    [SerializeField]
    protected float movementSpeedChangeRatio = 1;
    protected Movement movement;
    public override void Init(DebuffUpdater target)
    {
        base.Init(target);
        movement = target.GetComponent<Movement>();
    }
    protected override void UpdateDebuff()
    {
    }

    protected override void Remove()
    {
        base.Remove();
        movement.ResetSpeed();
    }
}