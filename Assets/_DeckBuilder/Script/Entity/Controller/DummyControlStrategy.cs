using System;
using UnityEngine;

[CreateAssetMenu(fileName = nameof(DummyControlStrategy), menuName = FileName.Controller + nameof(DummyControlStrategy))]
public class DummyControlStrategy : ControlStrategy
{
    public override void Initialize(Controller controller, Character character, IUIManager uiManager = null)
    {
        base.Initialize(controller, character, uiManager);
        character.On_Died += Character_On_Died;
    }

    private void Character_On_Died()
    {
        character.TakeDamage(-character.Health.MaxHealth);
    }

    public override void Control(float deltaTime)
    {

    }

    public override void Disable()
    {
        character.On_Died -= Character_On_Died;
    }
}
