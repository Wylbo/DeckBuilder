using UnityEngine;
using System;

[CreateAssetMenu(fileName = "AbilityMulitipleProjectile", menuName = FileName.Abilities + "AbilityMulitipleProjectile", order = 0)]
public class AbilityMulitipleProjectile : AbilityChanneled
{
	// poe2 bone storm
	protected override void DoCast(Vector3 worldPos)
	{
		base.DoCast(worldPos);
	}

	protected override void UpdateChanneling()
	{

	}
}