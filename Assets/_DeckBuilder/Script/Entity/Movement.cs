using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MG.Extend;
using UnityEngine.UIElements;

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
	private float HalfHeight => capsuleCollider.height / 2;

	public bool CanMove => canMove;
	public bool isMoving => agent.velocity.magnitude > 0;
	public Rigidbody Body => body;

	private void Reset()
	{
		body = GetComponent<Rigidbody>();
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
	}

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

		List<Vector3> dashPositions = CheckForWallAndSlopes(wantedDestination, dashData);

		for (int i = 0; i < dashPositions.Count; i++)
		{
			DebugDrawer.DrawSphere(dashPositions[i], 0.1f, Color.green, 1f);
		}

		return dashPositions;

	}

	private List<Vector3> CheckForWallAndSlopes(Vector3 wantedPosition, DashData dashData)
	{
		List<Vector3> dashPositions = new List<Vector3>() { transform.position };
		Vector3 wantedDirection = wantedPosition - dashPositions[^1];

		Physics.Raycast(transform.position, Vector3.down, out RaycastHit down, 2f, groundLayerMask);
		Vector3 groundNormal = down.normal;
		Debug.DrawRay(transform.position, groundNormal, Color.red, 1f);

		wantedDirection = Vector3.ProjectOnPlane(wantedDirection, groundNormal);
		wantedDirection = wantedDirection.normalized;

		Vector3 remaining = wantedPosition - dashPositions[^1];


		float agentRadius = NavMesh.GetSettingsByID(agent.agentTypeID).agentRadius;

		Vector3 point1 = transform.position - Vector3.up * HalfHeight / 2;
		Vector3 point2 = transform.position + Vector3.up * HalfHeight / 2;
		Debug.DrawLine(point1, point2, Color.magenta, 3f);
		bool forwardCheck = Physics.CapsuleCast(point1, point2, agent.radius, wantedDirection, out RaycastHit forwardHit, remaining.magnitude, dashData.blockingMask);

		Debug.DrawLine(transform.position - Vector3.up * HalfHeight, transform.position - Vector3.up * HalfHeight + wantedDirection * remaining.magnitude, forwardCheck ? Color.magenta : Color.blue, 3f);

		while (forwardCheck)
		{
			float hitAngle = Vector3.Angle(Vector3.up, forwardHit.normal);
			bool isSlope = hitAngle <= NavMesh.GetSettingsByID(agent.agentTypeID).agentSlope;

			Debug.DrawRay(forwardHit.point, forwardHit.normal * 2, Color.red, 3f);

			DebugDrawer.DrawSphere(forwardHit.point + (isSlope ? Vector3.up * HalfHeight : forwardHit.normal * agentRadius), .5f, Color.cyan, 3f);

			dashPositions.Add(forwardHit.point + (isSlope ? Vector3.up * HalfHeight : forwardHit.normal * agentRadius));

			if (!isSlope && !dashData.slideOnWalls)
			{
				remaining = Vector3.zero;
				break;
			}

			// calculate the remaining distance on the hitted plane
			remaining = wantedPosition - dashPositions[^1];
			remaining = Vector3.ProjectOnPlane(remaining, forwardHit.normal);
			wantedPosition = remaining + dashPositions[^1];

			wantedDirection = wantedPosition - dashPositions[^1];
			wantedDirection = wantedDirection.normalized;

			point1 = dashPositions[^1] - Vector3.up * HalfHeight / 2;
			point2 = dashPositions[^1] + Vector3.up * HalfHeight / 2;

			Debug.DrawLine(point1, point2, Color.magenta, 3f);
			forwardCheck = Physics.CapsuleCast(point1, point2, agent.radius, wantedDirection, out forwardHit, remaining.magnitude, dashData.blockingMask);

		}

		dashPositions.Add(dashPositions[^1] + wantedDirection * remaining.magnitude);

		return dashPositions;
	}

	private IEnumerator DashRoutine(List<Vector3> dashPosition, DashData dashData)
	{
		Vector3 startPos = transform.position;
		Vector3 nextPos = dashPosition[0];

		float dashDuration = dashData.dashDistance / dashData.dashSpeed;

		agent.enabled = false;
		body.interpolation = RigidbodyInterpolation.Interpolate;

		Vector3 lookAt = nextPos - startPos;
		lookAt.y = 0f;

		Quaternion lookRot = Vector3.SqrMagnitude(lookAt) == 0 ? Quaternion.LookRotation(transform.forward) : Quaternion.LookRotation(lookAt);
		transform.rotation = lookRot;

		int index = 0;
		float elapsedTime = 0;

		while (Vector3.Distance(body.position, dashPosition[^1]) > 0.001f)
		{
			elapsedTime += Time.deltaTime;
			float normalizedTime = Mathf.Clamp01(elapsedTime / dashDuration);
			float speed = dashData.dashCurve.Evaluate(normalizedTime) * dashData.dashSpeed;

			body.MovePosition(Vector3.MoveTowards(body.position, nextPos, speed * Time.deltaTime));

			if (Vector3.Distance(body.position, nextPos) <= 0.001f && index < dashPosition.Count - 1)
			{
				index++;
				startPos = nextPos;
				nextPos = dashPosition[index];

				Debug.DrawLine(startPos, nextPos, Color.green, 3f);

				lookAt = nextPos - startPos;
				lookAt.y = 0f;

				lookRot = Vector3.SqrMagnitude(lookAt) == 0 ? Quaternion.LookRotation(transform.forward) : Quaternion.LookRotation(lookAt);
				body.rotation = lookRot;

			}
			yield return null;
		}

		agent.enabled = true;
		body.interpolation = RigidbodyInterpolation.None;
		canMove = true;
	}
}
