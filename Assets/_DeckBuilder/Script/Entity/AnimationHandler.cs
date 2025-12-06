using UnityEngine;

/// <summary>
/// Centralizes animation parameter updates for an entity.
/// </summary>
public class AnimationHandler : MonoBehaviour
{
    [SerializeField] private Animator animator = null;
    [SerializeField] private Transform orientationTransform = null;
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string forwardParam = "Forward";
    [SerializeField] private string rightParam = "Right";

    private const float MinPlanarVelocitySqr = 0.0001f;
    private Vector3 latestWorldVelocity = Vector3.zero;

    private void Awake()
    {
        if (orientationTransform == null)
            orientationTransform = transform;
    }

    private void Reset()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (orientationTransform == null)
            orientationTransform = transform;
    }

    public void UpdateMovement(Vector3 worldVelocity)
    {
        latestWorldVelocity = worldVelocity;
    }

    private void LateUpdate()
    {
        ApplyMovementParameters();
    }

    private void ApplyMovementParameters()
    {
        if (animator == null || orientationTransform == null)
            return;

        animator.SetFloat(moveSpeedParam, latestWorldVelocity.magnitude);

        Vector3 planarVelocity = Vector3.ProjectOnPlane(latestWorldVelocity, Vector3.up);
        if (planarVelocity.sqrMagnitude > MinPlanarVelocitySqr)
        {
            Vector3 localDirection = orientationTransform.InverseTransformDirection(planarVelocity.normalized);
            animator.SetFloat(forwardParam, Mathf.Clamp(localDirection.z, -1f, 1f));
            animator.SetFloat(rightParam, Mathf.Clamp(localDirection.x, -1f, 1f) * localDirection.z);
        }
        else
        {
            animator.SetFloat(forwardParam, 0f);
            animator.SetFloat(rightParam, 0f);
        }
    }
}
