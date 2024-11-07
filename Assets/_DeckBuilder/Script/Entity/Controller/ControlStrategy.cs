using UnityEngine;

public abstract class ControlStrategy : ScriptableObject
{
	protected Controller controller;
	protected Character character;

	public virtual void Initialize(Controller controller, Character character)
	{
		this.controller = controller;
		this.character = character;
	}

	public abstract void Disable();

	public abstract void Control();

}
