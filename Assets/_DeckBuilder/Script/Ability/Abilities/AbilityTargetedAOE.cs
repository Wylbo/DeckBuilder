using UnityEngine;

[CreateAssetMenu(fileName = nameof(AbilityTargetedAOE), menuName = FileName.Abilities + nameof(AbilityTargetedAOE))]
public class AbilityTargetedAOE : Ability, IHasAOE
{
	[SerializeField]
	private Projectile projectile;

	#region IBaseAbilityModifier
	public override void ResetModifiers()
	{
		base.ResetModifiers();
		modifiedScale = 0;
	}
	#endregion

	#region IHasAOE
	protected float baseScale = 1;
	protected float modifiedScale = 0;
	public void AddAOEScale(float percent)
	{
		modifiedScale += percent;
	}

	public float GetModifiedScale()
	{
		float mult = 1 + modifiedScale;
		return baseScale * mult;
	}
	#endregion

	protected override void DoCast(Vector3 worldPos)
	{
		Caster.ProjectileLauncher.LaunchProjectile(projectile, worldPos, GetModifiedScale());
		base.DoCast(worldPos);
	}

}
