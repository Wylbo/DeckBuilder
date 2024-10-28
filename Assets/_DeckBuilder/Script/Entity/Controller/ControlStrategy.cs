using UnityEngine;

public abstract class ControlStrategy : ScriptableObject
{

	protected Controller controller;

	public virtual void Initialize(Controller controller)
	{
		this.controller = controller;
	}

	public abstract void Disable();

	public abstract void Control();

}
