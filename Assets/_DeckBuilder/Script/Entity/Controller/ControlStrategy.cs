using UnityEngine;

public abstract class ControlStrategy : ScriptableObject
{
	protected Controller controller;
	protected Character character;
	protected IUIManager UiManager { get; private set; }

	public virtual void Initialize(Controller controller, Character character, IUIManager uiManager = null)
	{
		this.controller = controller;
		this.character = character;
		UiManager = uiManager;
	}

	public abstract void Disable();

	public abstract void Control(float deltaTime);

}
