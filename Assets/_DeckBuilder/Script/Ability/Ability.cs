using UnityEngine;

[CreateAssetMenu(fileName = nameof(Ability), menuName = FileName.Ability + nameof(Ability))]
public abstract class Ability : ScriptableObject
{
	[SerializeField]
	private bool rotateCasterToCastDirection = true;
	public AbilityCaster Caster { get; private set; }
	public bool RotateCasterToCastDirection => rotateCasterToCastDirection;

	public virtual void Initialize(AbilityCaster caster)
	{
		Caster = caster;
	}

	public virtual void Disable()
	{
		Caster = null;
	}

	public abstract void Cast();
}
