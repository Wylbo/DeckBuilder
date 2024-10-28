using System;
using UnityEngine;

public abstract class MovingProjectile : Projectile
{
	protected override void Update()
	{
		base.Update();
		Move();
	}

	protected abstract void Move();
}
