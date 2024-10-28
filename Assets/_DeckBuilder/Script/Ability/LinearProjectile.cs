using UnityEngine;

public class LinearProjectile : MovingProjectile
{
	[SerializeField]
	private float maxSpeed;
	[SerializeField]
	private float acceleration;
	[SerializeField]
	private AnimationCurve accelerationProfile = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	private float currentSpeed;

	protected override void OnEnable()
	{
		base.OnEnable();
		currentSpeed = 0;
	}

	protected override void Move()
	{
		currentSpeed += accelerationProfile.Evaluate(TimeRatio) * acceleration * Time.deltaTime;

		currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);

		transform.position += transform.forward * currentSpeed * Time.deltaTime;
	}
}
