using UnityEngine;

[CreateAssetMenu(fileName = "DebuffMovementSpeed", menuName = FileName.Debuff + "DebuffMovementSpeed", order = 0)]
public class DebuffMovementSpeed : ScriptableDebuff
{
    [SerializeField]
    protected float movementSpeedChangeRatio = 1;

    public float MovementSpeedChangeRatio => movementSpeedChangeRatio;

    public override DebuffApplier InitDebuff(DebuffUpdater target)
    {
        return new DebuffApplierMovementSpeed(this, target);
    }
}

public class DebuffApplierMovementSpeed : DebuffApplier
{
    private DebuffMovementSpeed speedDebuff;
    private Movement movement;
    public DebuffApplierMovementSpeed(ScriptableDebuff debuff, DebuffUpdater debuffUpdater)
    : base(debuff, debuffUpdater)
    {
        speedDebuff = debuff as DebuffMovementSpeed;
        movement = debuffUpdater.GetComponent<Movement>();
    }

    public override void End()
    {
        movement.ResetSpeed();
    }

    protected override void ApplyEffect()
    {
        if (movement == null)
            return;

        movement.SpeedChangePercent(speedDebuff.MovementSpeedChangeRatio);
    }

    protected override void ApplyTick()
    {
    }
}