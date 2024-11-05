using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MG.Extend;
using UnityEngine.UIElements;
using Unity.Mathematics;
using Unity.VisualScripting;

/// <summary>
/// Component allowing an entity to move
/// </summary>

[RequireComponent(typeof(Rigidbody))]
public class Movement : MonoBehaviour
{
	[Serializable]
	public struct DashData
	{
		[SerializeField]
		public float dashDistance;
		[SerializeField]
		public float dashSpeed;
		[SerializeField]
		public bool slideOnWalls;
		[SerializeField]
		public AnimationCurve dashCurve;
		[SerializeField]
		public LayerMask blockingMask;
	}

	[SerializeField]
	private Rigidbody body = null;

	[SerializeField]
	private NavMeshAgent agent = null;
	[SerializeField]
	private CapsuleCollider capsuleCollider = null;
	[SerializeField]
	private LayerMask groundLayerMask;

	[SerializeField]
	private DashData defaultDashData = new DashData();


	private bool canMove = true;
	private Coroutine dashRoutine;
	private Vector3 wantedVelocity = Vector3.zero;
	private float baseMaxSpeed = 0;

	private float HalfHeight => capsuleCollider.height / 2;
	public bool CanMove => canMove;
	public bool IsMoving => wantedVelocity.magnitude > 0;
	public Rigidbody Body => body;

	private void Reset()
	{
		body = GetComponent<Rigidbody>();
	}

	private void Awake()
	{
		baseMaxSpeed = agent.speed;
		agent.updateRotation = false;
	}

	private void Update()
	{
		InstantTurn();
		wantedVelocity = agent.velocity;
	}

	public bool MoveTo(Vector3 worldTo)
	{
		if (!agent.enabled || !CanMove)
			return false;

		body.position = agent.nextPosition;
		return agent.SetDestination(worldTo);
	}

	public void StopMovement()
	{
		if (!agent.enabled)
			return;

		agent.ResetPath();
		agent.velocity = Vector3.zero;
		wantedVelocity = Vector3.zero;
	}

	public void DisableMovement()
	{
		StopMovement();
		canMove = false;
	}

	public void EnableMovement()
	{
		canMove = true;
	}

	public void SpeedChangePercent(float speedChangeRatio)
	{
		agent.speed = baseMaxSpeed * speedChangeRatio;
	}

	public void SpeedChange(float newSpeed)
	{
		agent.speed = newSpeed;
	}

	public void ResetSpeed()
	{
		agent.speed = baseMaxSpeed;
	}

	private void InstantTurn()
	{
		if (!agent.hasPath)
			return;

		Vector3 direction = agent.destination - transform.position;
		direction.y = 0;
		direction = direction.normalized;

		Quaternion rotation = quaternion.LookRotation(direction, Vector3.up);
		transform.rotation = rotation;
	}

	#region Dash
	public void Dash()
	{
		Dash(defaultDashData, transform.position + transform.forward);
	}

	public void Dash(Vector3 toward)
	{
		Dash(defaultDashData, toward);
	}

	public void Dash(DashData dashData)
	{
		Dash(dashData, transform.position + transform.forward);
	}

	public void Dash(DashData dashData, Vector3 toward)
	{
		StopMovement();
		canMove = false;

		if (dashRoutine != null)
			StopCoroutine(dashRoutine);

		List<Vector3> dashPositions = ComputeDashPositions(dashData, toward);

		dashRoutine = StartCoroutine(DashRoutine(dashPositions, dashData));
	}

	private List<Vector3> ComputeDashPositions(DashData dashData, Vector3 toward)
	{
		toward.y = transform.position.y;

		Vector3 direction = toward - transform.position;
		direction = direction.normalized;

		Vector3 wantedDestination = transform.position + direction * dashData.dashDistance;

		List<Vector3> dashPositions = CheckWalls(wantedDestination, dashData);

		for (int i = 0; i < dashPositions.Count; i++)
		{
			DebugDrawer.DrawSphere(dashPositions[i], 0.1f, Color.green, 1f);
		}

		return dashPositions;

	}

	private List<Vector3> CheckWalls(Vector3 wantedPosition, DashData dashData)
	{
		Vector3 groundNormal = GetStartingGroundNormal();

		List<Vector3> dashPositions = new List<Vector3>() { transform.position };
		Vector3 wantedDirection = wantedPosition - dashPositions[^1];

		wantedDirection = Vector3.ProjectOnPlane(wantedDirection, groundNormal);
		wantedDirection = wantedDirection.normalized;

		Vector3 remaining = wantedPosition - dashPositions[^1];
		float agentRadius = NavMesh.GetSettingsByID(agent.agentTypeID).agentRadius + 0.1f;

		Vector3 capsuleCastPoint1 = transform.position - Vector3.up * HalfHeight / 2;
		Vector3 capsuleCastPoint2 = transform.position + Vector3.up * HalfHeight / 2;
		Debug.DrawLine(capsuleCastPoint1, capsuleCastPoint2, Color.magenta, 3f);

		bool forwardCheck = Physics.CapsuleCast(capsuleCastPoint1, capsuleCastPoint2, agent.radius, wantedDirection,
			out RaycastHit forwardHit, remaining.magnitude, dashData.blockingMask);

		// calculate positions to slide on walls
		// TODO : recursively look for the end of the wall 
		// while (forwardCheck)
		// {
		// 	//Draw normal hit
		// 	Debug.DrawRay(forwardHit.point, forwardHit.normal * 2, Color.red, 3f);

		// 	Vector3 hitpoint = forwardHit.point;
		// 	hitpoint.y = body.position.y;

		// 	DebugDrawer.DrawCapusle(forwardHit.point + forwardHit.normal * agentRadius, agentRadius - 0.1f, HalfHeight * 2, Color.green, 3f);

		// 	dashPositions.Add(hitpoint + forwardHit.normal * agentRadius);

		// 	if (!dashData.slideOnWalls)
		// 	{
		// 		remaining = Vector3.zero;
		// 		break;
		// 	}

		// 	// calculate the remaining distance on the hitted plane
		// 	remaining = wantedPosition - dashPositions[^1];
		// 	remaining = Vector3.ProjectOnPlane(remaining, forwardHit.normal);

		// 	wantedPosition = remaining + dashPositions[^1];

		// 	wantedDirection = wantedPosition - dashPositions[^1];
		// 	wantedDirection = wantedDirection.normalized;

		// 	capsuleCastPoint1 = dashPositions[^1] - Vector3.up * HalfHeight / 2;
		// 	capsuleCastPoint2 = dashPositions[^1] + Vector3.up * HalfHeight / 2;
		// 	Debug.DrawLine(capsuleCastPoint1, capsuleCastPoint2, Color.magenta, 3f);

		// 	forwardCheck = Physics.CapsuleCast(capsuleCastPoint1, capsuleCastPoint2, agent.radius, wantedDirection,
		// 		out forwardHit, remaining.magnitude, dashData.blockingMask);
		// }

		dashPositions.Add(dashPositions[^1] + wantedDirection * remaining.magnitude);

		return dashPositions;
	}

	private Vector3 GetStartingGroundNormal()
	{
		Physics.Raycast(transform.position, Vector3.down, out RaycastHit down, 2f, groundLayerMask);
		Vector3 groundNormal = down.normal;
		Debug.DrawRay(transform.position, groundNormal, Color.red, 1f);
		return groundNormal;
	}

	private IEnumerator DashRoutine(DashData dashData, Vector3 toward)
	{
		Vector3 startPos = agent.nextPosition;
		Vector3 direction = (toward - startPos).normalized;
		float dashDuration = dashData.dashDistance / dashData.dashSpeed;
		float elapsedTime = 0;

		while (elapsedTime < dashDuration)
		{
			// Increment elapsed time.
			elapsedTime += Time.deltaTime;
			float normalizedTime = Mathf.Clamp01(elapsedTime / dashDuration);
			float curveValue = dashData.dashCurve.Evaluate(normalizedTime);

			// Calculate movement for this frame based on the dash speed and curve
			float movementAmount = dashData.dashSpeed * Time.deltaTime; // How much to move this frame
			Vector3 movement = direction * movementAmount * curveValue; // Apply curve value

			// Check if the path is clear
			if (IsPathClear(startPos, agent.nextPosition + movement))
			{
				// If the path is clear, move the agent
				agent.nextPosition += movement;
			}
			else
			{
				// If there's an obstacle, steer toward the target
				Vector3 obstacleAvoidanceDirection = (toward - agent.nextPosition).normalized;
				agent.nextPosition += obstacleAvoidanceDirection * (dashData.dashSpeed * Time.deltaTime);
			}


			yield return null;
		}
		canMove = true;
	}
	private bool IsPathClear(Vector3 start, Vector3 end)
	{
		// Check for obstacles between start and end.
		return !Physics.Linecast(start, end, out RaycastHit hit);
	}


	private IEnumerator DashRoutine(List<Vector3> dashPositions, DashData dashData)
	{
		SetBodyDashRotation(dashPositions);

		float dashDuration = dashData.dashDistance / dashData.dashSpeed;
		float elapsedTime = 0;
		float normalizedTime;
		float curvePosition;
		Vector3 nextPosition;
		Vector3 startObstrPos = Vector3.zero;
		Vector3 endObstrPos = agent.nextPosition;
		bool isObstruted = false;
		bool wasObstruted = false;

		while (elapsedTime < dashDuration)
		{
			elapsedTime += Time.deltaTime;
			normalizedTime = Mathf.Clamp01(elapsedTime / dashDuration);

			curvePosition = dashData.dashCurve.Evaluate(normalizedTime);
			nextPosition = StickNextPositionToGround(dashPositions, curvePosition);

			if (IsPathObstructed(agent.nextPosition, nextPosition, out RaycastHit hit))
			{
				isObstruted = true;
				if (startObstrPos == Vector3.zero)
					startObstrPos = agent.nextPosition;

				nextPosition = SlideOnWall(agent.nextPosition, nextPosition, hit);
				// dashPositions = UpdateDashPath(dashPositions, agent.nextPosition, nextPosition);
			}
			// else
			// {
			// 	isObstruted = false;
			// }

			// if (wasObstruted && !isObstruted)
			// {
			// 	endObstrPos = agent.nextPosition;
			// 	float obstrutedTraveledDistance = Vector3.Distance(startObstrPos, endObstrPos);

			// }

			// wasObstruted = isObstruted;
			agent.nextPosition = nextPosition;

			yield return null;
		}

		canMove = true;
	}

	private void SetBodyDashRotation(List<Vector3> dashPositions)
	{
		Vector3 startPos = transform.position;
		Vector3 nextPos = dashPositions[0];

		Vector3 lookAt = nextPos - startPos;
		lookAt.y = 0f;
		Quaternion lookRot = Vector3.SqrMagnitude(lookAt) == 0 ? Quaternion.LookRotation(transform.forward) : Quaternion.LookRotation(lookAt);
		body.rotation = lookRot;
	}

	private Vector3 StickNextPositionToGround(List<Vector3> dashPositions, float curvePosition)
	{
		Vector3 nextPosition = InterpolatePath(dashPositions, curvePosition);
		if (Physics.SphereCast(body.position, agent.radius, Vector3.down, out RaycastHit groundHit, NavMesh.GetSettingsByID(agent.agentTypeID).agentClimb, groundLayerMask))
		{
			nextPosition.y = groundHit.point.y + HalfHeight;
		}

		return nextPosition;
	}

	private bool IsPathObstructed(Vector3 from, Vector3 to, out RaycastHit hit)
	{
		return Physics.Raycast(from, (to - from).normalized, out hit, Vector3.Distance(from, to));
	}

	private Vector3 SlideOnWall(Vector3 from, Vector3 to, RaycastHit hit)
	{
		// Vector3 slideDirection = Vector3.Cross(hit.normal, (to - from).normalized).normalized;
		Vector3 slideDirection = Vector3.ProjectOnPlane(to - from, hit.normal);

		return from + slideDirection * 0.1f;
	}

	private Vector3 InterpolatePath(List<Vector3> path, float t)
	{
		float segmentCount = path.Count - 1;
		float segmentIndex = t * segmentCount;
		int currentSegment = Mathf.FloorToInt(segmentIndex);
		int nextSegment = Mathf.Clamp(currentSegment + 1, 0, path.Count - 1);

		float segmentFraction = segmentIndex - currentSegment;

		return Vector3.Lerp(path[currentSegment], path[nextSegment], segmentFraction);
	}

	private List<Vector3> UpdateDashPath(List<Vector3> originalPath, Vector3 pos, Vector3 nextPos)
	{
		List<Vector3> updatedPath = new List<Vector3>(originalPath) { nextPos };

		Vector3 temp = updatedPath[^1];
		updatedPath[^1] = updatedPath[^2];
		updatedPath[^2] = temp;

		return updatedPath;
	}
	#endregion
}
