using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Centralizes animation parameter updates for an entity and plays ability clips via Playables.
/// </summary>
public class AnimationHandler : MonoBehaviour
{
    private const float MinPlanarVelocitySqr = 0.0001f;
    private const int BaseLayerIndex = 0;
    private const int AbilityLayerIndex = 1;

    [Header("Animator")]
    [SerializeField] private Animator animator = null;
    [SerializeField] private Transform orientationTransform = null;
    [SerializeField] private string moveSpeedParam = "MoveSpeed";
    [SerializeField] private string forwardParam = "Forward";
    [SerializeField] private string rightParam = "Right";

    [Header("Ability Animation")]
    [SerializeField] private AvatarMask upperBodyMask = null;
    [SerializeField, Min(0f)] private float defaultBlendInDuration = 0.1f;
    [SerializeField, Min(0f)] private float defaultBlendOutDuration = 0.1f;

    private Vector3 latestWorldVelocity = Vector3.zero;
    private PlayableGraph playableGraph;
    private AnimationLayerMixerPlayable layerMixer;
    private AnimatorControllerPlayable controllerPlayable;
    private AnimationPlayableOutput playableOutput;
    private AnimationClipPlayable abilityPlayable;
    private Coroutine blendRoutine;
    private AbilityAnimationData activeAbilityData;
    private bool graphInitialized;
    private bool initialApplyRootMotion;

    private int moveSpeedParamHash;
    private int forwardParamHash;
    private int rightParamHash;

    private void Awake()
    {
        if (orientationTransform == null)
            orientationTransform = transform;

        CacheParameterHashes();
        initialApplyRootMotion = animator != null && animator.applyRootMotion;
    }

    private void Reset()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (orientationTransform == null)
            orientationTransform = transform;
        CacheParameterHashes();
    }

    private void OnValidate()
    {
        CacheParameterHashes();
    }

    private void OnDisable()
    {
        StopAbilityAnimationImmediate();
        DestroyGraph();
    }

    private void OnDestroy()
    {
        StopAbilityAnimationImmediate();
        DestroyGraph();
    }

    public void UpdateMovement(Vector3 worldVelocity)
    {
        latestWorldVelocity = worldVelocity;
    }

    /// <summary>
    /// Plays the provided ability animation clip using the Playables graph.
    /// </summary>
    public void PlayAbilityAnimation(AbilityAnimationData animationData)
    {
        if (animationData == null || animationData.Clip == null || animator == null)
            return;

        if (!EnsureGraph())
            return;

        if (blendRoutine != null)
            StopCoroutine(blendRoutine);

        StopAbilityAnimationImmediate();

        activeAbilityData = animationData;
        animator.applyRootMotion = animationData.ApplyRootMotion;

        var clip = animationData.Clip;
        abilityPlayable = AnimationClipPlayable.Create(playableGraph, clip);
        abilityPlayable.SetSpeed(Mathf.Approximately(animationData.PlaybackSpeed, 0f) ? 1f : animationData.PlaybackSpeed);
        abilityPlayable.SetApplyFootIK(true);
        if (clip != null)
            clip.wrapMode = animationData.Loop ? WrapMode.Loop : WrapMode.Once;
        abilityPlayable.SetDuration(animationData.Loop ? double.PositiveInfinity : clip.length);
        abilityPlayable.SetTime(0d);

        playableGraph.Connect(abilityPlayable, 0, layerMixer, AbilityLayerIndex);
        layerMixer.SetLayerAdditive(AbilityLayerIndex, false);
        AvatarMask mask = animationData.Body == AbilityAnimationBody.UpperBody ? upperBodyMask : null;
        if (mask != null)
            layerMixer.SetLayerMaskFromAvatarMask(AbilityLayerIndex, mask);
        ApplyLayerWeights(0f);

        float blendIn = animationData.BlendInDuration > 0f ? animationData.BlendInDuration : defaultBlendInDuration;
        blendRoutine = StartCoroutine(BlendAbilityWeight(0f, 1f, blendIn, false));
    }

    /// <summary>
    /// Stops the active ability animation with a blend out.
    /// </summary>
    public void StopAbilityAnimation(AbilityAnimationData animationData = null)
    {
        if (!abilityPlayable.IsValid())
            return;

        if (blendRoutine != null)
            StopCoroutine(blendRoutine);

        float blendOut = animationData?.BlendOutDuration
                         ?? activeAbilityData?.BlendOutDuration
                         ?? defaultBlendOutDuration;

        blendRoutine = StartCoroutine(BlendAbilityWeight(
            layerMixer.IsValid() ? layerMixer.GetInputWeight(AbilityLayerIndex) : 0f,
            0f,
            blendOut,
            true));
    }

    private void LateUpdate()
    {
        ApplyMovementParameters();
    }

    private void ApplyMovementParameters()
    {
        if (orientationTransform == null)
            return;

        float moveSpeed = latestWorldVelocity.magnitude;
        SetFloat(moveSpeedParamHash, moveSpeed);

        Vector3 planarVelocity = Vector3.ProjectOnPlane(latestWorldVelocity, Vector3.up);
        if (planarVelocity.sqrMagnitude > MinPlanarVelocitySqr)
        {
            Vector3 localDirection = orientationTransform.InverseTransformDirection(planarVelocity.normalized);
            SetFloat(forwardParamHash, Mathf.Clamp(localDirection.z, -1f, 1f));
            SetFloat(rightParamHash, Mathf.Clamp(localDirection.x, -1f, 1f) * localDirection.z);
        }
        else
        {
            SetFloat(forwardParamHash, 0f);
            SetFloat(rightParamHash, 0f);
        }
    }

    private void CacheParameterHashes()
    {
        moveSpeedParamHash = Animator.StringToHash(moveSpeedParam);
        forwardParamHash = Animator.StringToHash(forwardParam);
        rightParamHash = Animator.StringToHash(rightParam);
    }

    private void SetFloat(int paramHash, float value)
    {
        if (controllerPlayable.IsValid())
            controllerPlayable.SetFloat(paramHash, value);

        if (animator != null)
            animator.SetFloat(paramHash, value);
    }

    private bool EnsureGraph()
    {
        if (graphInitialized)
            return true;

        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        playableGraph = PlayableGraph.Create($"{name}_AnimationGraph");
        controllerPlayable = AnimatorControllerPlayable.Create(playableGraph, animator.runtimeAnimatorController);
        layerMixer = AnimationLayerMixerPlayable.Create(playableGraph, 2);
        playableGraph.Connect(controllerPlayable, 0, layerMixer, BaseLayerIndex);
        layerMixer.SetInputWeight(BaseLayerIndex, 1f);
        layerMixer.SetInputWeight(AbilityLayerIndex, 0f);

        playableOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationHandlerOutput", animator);
        playableOutput.SetSourcePlayable(layerMixer);

        playableGraph.Play();
        graphInitialized = true;
        return true;
    }

    private void DestroyGraph()
    {
        if (!playableGraph.IsValid())
            return;

        playableGraph.Destroy();
        graphInitialized = false;
    }

    private IEnumerator BlendAbilityWeight(float from, float to, float duration, bool destroyOnComplete)
    {
        float elapsed = 0f;
        float clampedDuration = Mathf.Max(0f, duration);

        while (elapsed < clampedDuration)
        {
            elapsed += Time.deltaTime;
            float t = clampedDuration > 0f ? Mathf.Clamp01(elapsed / clampedDuration) : 1f;
            float weight = Mathf.Lerp(from, to, t);
            ApplyLayerWeights(weight);
            yield return null;
        }

        ApplyLayerWeights(to);

        if (destroyOnComplete)
            StopAbilityAnimationImmediate();
    }

    private void ApplyLayerWeights(float abilityWeight)
    {
        if (!layerMixer.IsValid())
            return;

        float clamped = Mathf.Clamp01(abilityWeight);
        float baseWeight = activeAbilityData != null && activeAbilityData.Body == AbilityAnimationBody.FullBody
            ? 1f - clamped
            : 1f;

        layerMixer.SetInputWeight(BaseLayerIndex, baseWeight);
        layerMixer.SetInputWeight(AbilityLayerIndex, clamped);
    }

    private void StopAbilityAnimationImmediate()
    {
        if (layerMixer.IsValid() && layerMixer.GetInputCount() > AbilityLayerIndex)
            layerMixer.DisconnectInput(AbilityLayerIndex);

        if (abilityPlayable.IsValid())
        {
            abilityPlayable.Destroy();
            abilityPlayable = default;
        }

        ApplyLayerWeights(0f);
        activeAbilityData = null;
        if (animator != null)
            animator.applyRootMotion = initialApplyRootMotion;
    }
}
