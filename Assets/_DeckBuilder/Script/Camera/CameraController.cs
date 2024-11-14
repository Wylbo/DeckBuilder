using UnityEngine;
public class CameraController : MonoBehaviour
{
	[SerializeField]
	private Camera _camera;
	[SerializeField]
	private Transform targetToFollow;
	[SerializeField]
	private Transform lookAtTarget;
	[SerializeField]
	private float cameraLag;

	public Camera Camera => _camera;

	private Vector3 offsetFromTarget = default;
	private Vector3 cameraPosition = default;

	private void Awake()
	{
		UnityEngine.Camera.SetupCurrent(Camera);

		offsetFromTarget = transform.position - targetToFollow.position;
		cameraPosition = transform.position;
	}

	private void LateUpdate()
	{
		FollowTarget();
		// LookAtTarget();
	}

	private void FollowTarget()
	{
		Vector3 wantedPos = offsetFromTarget + targetToFollow.position;
		Vector3 smoothedPos = Vector3.Lerp(cameraPosition, wantedPos, Time.deltaTime * cameraLag);

		transform.position = smoothedPos;
		cameraPosition = smoothedPos;
	}

	private void LookAtTarget()
	{
		Camera.transform.LookAt(lookAtTarget, Vector3.up);
	}
}
