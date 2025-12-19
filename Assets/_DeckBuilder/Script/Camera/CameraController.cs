using UnityEngine;
using UnityEngine.Serialization;

public class CameraController : MonoBehaviour
{
    #region Fields
    [FormerlySerializedAs("_camera")]
    [SerializeField] private Camera cameraComponent;
    [SerializeField] private Transform targetToFollow;
    [SerializeField] private Transform lookAtTarget;
    [SerializeField] private float cameraLag;
    #endregion

    #region Private Members
    private Vector3 offsetFromTarget = Vector3.zero;
    private Vector3 cameraPosition = Vector3.zero;
    #endregion

    #region Getters
    public Camera Camera => cameraComponent;
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        ValidateSerializedReferences();
        CacheInitialCameraState();
    }

    private void OnValidate()
    {
        ValidateSerializedReferences();
    }

    private void LateUpdate()
    {
        FollowTarget();
        LookAtTarget();
    }
    #endregion

    #region Public Methods
    #endregion

    #region Private Methods
    private void ValidateSerializedReferences()
    {
        if (cameraComponent == null)
        {
            cameraComponent = GetComponent<Camera>();
        }

        if (targetToFollow == null)
        {
            Debug.LogError($"{nameof(CameraController)} requires a target to follow.", this);
            enabled = false;
            return;
        }

        if (cameraComponent == null)
        {
            Debug.LogError($"{nameof(CameraController)} requires a {nameof(Camera)} reference.", this);
            enabled = false;
            return;
        }

        enabled = true;
    }

    private void CacheInitialCameraState()
    {
        if (!enabled)
        {
            return;
        }

        Camera.SetupCurrent(cameraComponent);

        offsetFromTarget = transform.position - targetToFollow.position;
        cameraPosition = transform.position;
    }

    private void FollowTarget()
    {
        if (!enabled || targetToFollow == null)
        {
            return;
        }

        Vector3 desiredPosition = targetToFollow.position + offsetFromTarget;
        Vector3 smoothedPosition = Vector3.Lerp(cameraPosition, desiredPosition, Time.deltaTime * cameraLag);

        transform.position = smoothedPosition;
        cameraPosition = smoothedPosition;
    }

    private void LookAtTarget()
    {
        if (!enabled || cameraComponent == null || lookAtTarget == null)
        {
            return;
        }

        cameraComponent.transform.LookAt(lookAtTarget, Vector3.up);
    }
    #endregion
}
