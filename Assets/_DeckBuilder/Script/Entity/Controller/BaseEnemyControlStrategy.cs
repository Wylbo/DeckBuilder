using TheKiwiCoder;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(BaseEnemyControlStrategy), menuName = FileName.Controller + nameof(BaseEnemyControlStrategy), order = 0)]
public class BaseEnemyControlStrategy : ControlStrategy
{
    protected BehaviourTreeInstance bt;

    public override void Initialize(Controller controller, Character character, IUIManager uiManager = null)
    {
        base.Initialize(controller, character, uiManager);
        bt = character.GetComponent<BehaviourTreeInstance>();
        bt.tickMode = BehaviourTreeInstance.TickMode.None;
    }

    public override void Control(float deltaTime)
    {
        bt.ManualTick(deltaTime);
    }

    public override void Disable()
    {

    }
}
