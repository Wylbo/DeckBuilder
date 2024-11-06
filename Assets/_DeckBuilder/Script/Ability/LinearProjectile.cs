using UnityEngine;

public class LinearProjectile : MovingProjectile
{
	[SerializeField]
	private float maxSpeed;
	[SerializeField]
	private bool hasAcceleration;
	[SerializeField]
	private float acceleration;
	[SerializeField]
	private AnimationCurve accelerationProfile = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	public float MaxSpeed => maxSpeed;
	private float currentSpeed;
	private float traveledDistance;
	private Vector3 startPos;
	private Vector3 endPos;

	protected override void OnEnable()
	{
		base.OnEnable();
		currentSpeed = 0;
		startPos = transform.position;
		traveledDistance = 0;
	}

	protected override void Move()
	{
		if (hasAcceleration)
		{
			currentSpeed += accelerationProfile.Evaluate(TimeRatio) * acceleration * Time.deltaTime;

			currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
		}
		else
		{
			currentSpeed = maxSpeed;
		}

		transform.position += transform.forward * currentSpeed * Time.deltaTime;
	}

	protected override void Kill()
	{
		endPos = transform.position;
		traveledDistance = Vector3.Distance(startPos, endPos);
		base.Kill();
	}
}
