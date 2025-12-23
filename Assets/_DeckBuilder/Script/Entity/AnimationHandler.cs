using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Centralizes animation parameter updates for an entity and plays ability clips via Playables.
/// </summary>
public class AnimationHandler : NetworkBehaviour
{
    #region Fields
    private const float MinPlanarVelocitySqr = 0.0001f;
    private const int BaseLayerIndex = 0;
    private const int AnimationLayerIndex = 1;
    private const int MixerInputCount = 2;

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

    private readonly NetworkVariable<Vector3> networkedVelocity = new NetworkVariable<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
    private readonly NetworkVariable<AnimationStatePayload> networkedAnimationState = new NetworkVariable<AnimationStatePayload>(writePerm: NetworkVariableWritePermission.Owner);
    #endregion

    #region Private Members
    private readonly Dictionary<int, AnimationClip> clipLookup = new Dictionary<int, AnimationClip>();

    private Vector3 latestWorldVelocity = Vector3.zero;
    private PlayableGraph playableGraph;
    private AnimationLayerMixerPlayable layerMixer;
    private AnimatorControllerPlayable controllerPlayable;
    private AnimationPlayableOutput playableOutput;
    private AnimationClipPlayable playableClip;
    private Coroutine blendRoutine;
    private bool hasActiveAnimation;
    private AbilityAnimationBody activeBody = AbilityAnimationBody.UpperBody;
    private float activeBlendOutDuration;
    private int activeClipHash;
    private FixedString64Bytes activeClipName;
    private bool graphInitialized;
    private bool initialApplyRootMotion;

    private int moveSpeedParamHash;
    private int forwardParamHash;
    private int rightParamHash;
    private bool callbacksRegistered;
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (orientationTransform == null)
        {
            orientationTransform = transform;
        }

        CacheParameterHashes();
        BuildClipLookupFromAnimator();
        initialApplyRootMotion = animator != null && animator.applyRootMotion;

        SubscribeNetworkCallbacks();
    }

    private void Reset()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (orientationTransform == null)
        {
            orientationTransform = transform;
        }

        CacheParameterHashes();
        BuildClipLookupFromAnimator();
    }

    private void OnValidate()
    {
        CacheParameterHashes();
        BuildClipLookupFromAnimator();
    }

    private void OnDisable()
    {
        StopAnimationImmediate();
        DestroyGraph();
        UnsubscribeNetworkCallbacks();
    }

    public override void OnDestroy()
    {
        StopAnimationImmediate();
        DestroyGraph();
        UnsubscribeNetworkCallbacks();
    }

    private void LateUpdate()
    {
        Vector3 velocity = IsOwner ? latestWorldVelocity : networkedVelocity.Value;
        ApplyMovementParameters(velocity);
    }
    #endregion

    #region Public Methods
    public void UpdateMovement(Vector3 worldVelocity)
    {
        latestWorldVelocity = worldVelocity;
        if (!IsOwner)
        {
            return;
        }

        if (ShouldSendVelocity(worldVelocity))
        {
            networkedVelocity.Value = worldVelocity;
        }
    }

    /// <summary>
    /// Plays the provided animation clip using the Playables graph.
    /// </summary>
    public void PlayAnimation(AnimationData animationData)
    {
        if (!IsOwner)
        {
            return;
        }

        if (animationData == null || animationData.Clip == null || animator == null)
        {
            return;
        }

        AnimationStatePayload payload = AnimationStatePayload.From(animationData, true, defaultBlendOutDuration);
        PlayAnimationInternal(animationData.Clip, payload);
        networkedAnimationState.Value = payload;
    }

    /// <summary>
    /// Stops the active ability animation with a blend out.
    /// </summary>
    public void StopAnimation(AnimationData animationData = null)
    {
        if (!IsOwner)
        {
            return;
        }

        float blendOut = defaultBlendOutDuration;
        if (animationData != null)
        {
            blendOut = animationData.BlendOutDuration;
        }
        else if (hasActiveAnimation)
        {
            blendOut = activeBlendOutDuration;
        }

        AnimationStatePayload payload = AnimationStatePayload.From(animationData, false, blendOut);
        if (animationData == null && hasActiveAnimation)
        {
            payload.ClipHash = activeClipHash;
            payload.ClipName = activeClipName;
            payload.Body = activeBody;
            payload.BlendOut = blendOut;
        }

        StopAnimationFromPayload(payload);
        networkedAnimationState.Value = payload;
    }
    #endregion

    #region Private Methods
    private void ApplyMovementParameters(Vector3 worldVelocity)
    {
        if (orientationTransform == null)
        {
            return;
        }

        float moveSpeed = worldVelocity.magnitude;
        SetFloat(moveSpeedParamHash, moveSpeed);

        Vector3 planarVelocity = Vector3.ProjectOnPlane(worldVelocity, Vector3.up);
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
        {
            controllerPlayable.SetFloat(paramHash, value);
        }

        if (animator != null)
        {
            animator.SetFloat(paramHash, value);
        }
    }

    private bool EnsureGraph()
    {
        if (graphInitialized)
        {
            return true;
        }

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return false;
        }

        playableGraph = PlayableGraph.Create($"{name}_AnimationGraph");
        controllerPlayable = AnimatorControllerPlayable.Create(playableGraph, animator.runtimeAnimatorController);
        layerMixer = AnimationLayerMixerPlayable.Create(playableGraph, MixerInputCount);
        playableGraph.Connect(controllerPlayable, 0, layerMixer, BaseLayerIndex);
        layerMixer.SetInputWeight(BaseLayerIndex, 1f);
        layerMixer.SetInputWeight(AnimationLayerIndex, 0f);

        playableOutput = AnimationPlayableOutput.Create(playableGraph, "AnimationHandlerOutput", animator);
        playableOutput.SetSourcePlayable(layerMixer);

        playableGraph.Play();
        graphInitialized = true;
        return true;
    }

    private void DestroyGraph()
    {
        if (!playableGraph.IsValid())
        {
            return;
        }

        playableGraph.Destroy();
        graphInitialized = false;
    }

    private IEnumerator BlendWeight(float from, float to, float duration, bool destroyOnComplete)
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
        {
            StopAnimationImmediate();
        }
    }

    private void ApplyLayerWeights(float abilityWeight)
    {
        if (!layerMixer.IsValid())
        {
            return;
        }

        float clamped = Mathf.Clamp01(abilityWeight);
        float baseWeight = hasActiveAnimation && activeBody == AbilityAnimationBody.FullBody ? 1f - clamped : 1f;

        layerMixer.SetInputWeight(BaseLayerIndex, baseWeight);
        layerMixer.SetInputWeight(AnimationLayerIndex, clamped);
    }

    private void StopAnimationImmediate()
    {
        if (layerMixer.IsValid() && layerMixer.GetInputCount() > AnimationLayerIndex)
        {
            layerMixer.DisconnectInput(AnimationLayerIndex);
        }

        if (playableClip.IsValid())
        {
            playableClip.Destroy();
            playableClip = default;
        }

        ApplyLayerWeights(0f);
        hasActiveAnimation = false;
        activeBlendOutDuration = 0f;
        activeClipHash = 0;
        activeClipName = default;
        if (animator != null)
        {
            animator.applyRootMotion = initialApplyRootMotion;
        }
    }

    private void PlayAnimationInternal(AnimationClip clip, AnimationStatePayload payload)
    {
        if (clip == null || animator == null)
        {
            return;
        }

        if (!EnsureGraph())
        {
            return;
        }

        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
        }

        StopAnimationImmediate();

        hasActiveAnimation = true;
        activeBody = payload.Body;
        activeBlendOutDuration = payload.BlendOut > 0f ? payload.BlendOut : defaultBlendOutDuration;
        activeClipHash = payload.ClipHash;
        activeClipName = payload.ClipName;
        animator.applyRootMotion = payload.ApplyRootMotion;

        playableClip = AnimationClipPlayable.Create(playableGraph, clip);
        float speed = Mathf.Approximately(payload.PlaybackSpeed, 0f) ? 1f : payload.PlaybackSpeed;
        playableClip.SetSpeed(speed);
        playableClip.SetApplyFootIK(true);
        clip.wrapMode = payload.Loop ? WrapMode.Loop : WrapMode.Once;
        playableClip.SetDuration(payload.Loop ? double.PositiveInfinity : clip.length);
        playableClip.SetTime(0d);

        playableGraph.Connect(playableClip, 0, layerMixer, AnimationLayerIndex);
        layerMixer.SetLayerAdditive(AnimationLayerIndex, false);
        AvatarMask mask = payload.Body == AbilityAnimationBody.UpperBody ? upperBodyMask : null;
        if (mask != null)
        {
            layerMixer.SetLayerMaskFromAvatarMask(AnimationLayerIndex, mask);
        }

        ApplyLayerWeights(0f);

        float blendIn = payload.BlendIn > 0f ? payload.BlendIn : defaultBlendInDuration;
        blendRoutine = StartCoroutine(BlendWeight(0f, 1f, blendIn, false));
    }

    private void StopAnimationFromPayload(AnimationStatePayload payload)
    {
        if (!playableClip.IsValid() || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (blendRoutine != null)
        {
            StopCoroutine(blendRoutine);
        }

        float currentWeight = layerMixer.IsValid() ? layerMixer.GetInputWeight(AnimationLayerIndex) : 0f;
        float blendOut = payload.BlendOut > 0f ? payload.BlendOut : defaultBlendOutDuration;

        blendRoutine = StartCoroutine(BlendWeight(
            currentWeight,
            0f,
            blendOut,
            true));
    }

    private void SubscribeNetworkCallbacks()
    {
        if (callbacksRegistered)
        {
            return;
        }

        networkedVelocity.OnValueChanged += HandleNetworkVelocityChanged;
        networkedAnimationState.OnValueChanged += HandleNetworkAnimationStateChanged;
        callbacksRegistered = true;
    }

    private void UnsubscribeNetworkCallbacks()
    {
        if (!callbacksRegistered)
        {
            return;
        }

        networkedVelocity.OnValueChanged -= HandleNetworkVelocityChanged;
        networkedAnimationState.OnValueChanged -= HandleNetworkAnimationStateChanged;
        callbacksRegistered = false;
    }

    private void HandleNetworkVelocityChanged(Vector3 previous, Vector3 current)
    {
        if (IsOwner)
        {
            return;
        }

        latestWorldVelocity = current;
    }

    private void HandleNetworkAnimationStateChanged(AnimationStatePayload previous, AnimationStatePayload current)
    {
        if (IsOwner)
        {
            return;
        }

        if (current.IsPlaying)
        {
            AnimationClip clip = ResolveAnimationClip(current.ClipHash, current.ClipName);
            if (clip == null)
            {
                StopAnimationImmediate();
                return;
            }

            PlayAnimationInternal(clip, current);
            return;
        }

        StopAnimationFromPayload(current);
    }

    private AnimationClip ResolveAnimationClip(int clipHash, FixedString64Bytes clipName)
    {
        if (clipHash != 0 && clipLookup.TryGetValue(clipHash, out AnimationClip cachedClip))
        {
            return cachedClip;
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] controllerClips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < controllerClips.Length; i++)
            {
                AnimationClip candidate = controllerClips[i];
                if (candidate == null)
                {
                    continue;
                }

                int candidateHash = Animator.StringToHash(candidate.name);
                if (candidateHash == clipHash || (!clipName.IsEmpty && candidate.name == clipName.ToString()))
                {
                    clipLookup[candidateHash] = candidate;
                    return candidate;
                }
            }
        }

        if (!clipName.IsEmpty)
        {
            AnimationClip resourceClip = Resources.Load<AnimationClip>(clipName.ToString());
            if (resourceClip != null)
            {
                int resourceHash = Animator.StringToHash(resourceClip.name);
                clipLookup[resourceHash] = resourceClip;
                return resourceClip;
            }
        }

        return null;
    }

    private void BuildClipLookupFromAnimator()
    {
        clipLookup.Clear();

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimationClip[] controllerClips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < controllerClips.Length; i++)
        {
            AnimationClip clip = controllerClips[i];
            if (clip == null)
            {
                continue;
            }

            int hash = Animator.StringToHash(clip.name);
            if (!clipLookup.ContainsKey(hash))
            {
                clipLookup.Add(hash, clip);
            }
        }
    }

    private bool ShouldSendVelocity(Vector3 worldVelocity)
    {
        Vector3 delta = networkedVelocity.Value - worldVelocity;
        return delta.sqrMagnitude > MinPlanarVelocitySqr;
    }

    private struct AnimationStatePayload : INetworkSerializable
    {
        public int ClipHash;
        public FixedString64Bytes ClipName;
        public bool IsPlaying;
        public bool Loop;
        public float BlendIn;
        public float BlendOut;
        public float PlaybackSpeed;
        public bool ApplyRootMotion;
        public AbilityAnimationBody Body;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClipHash);
            serializer.SerializeValue(ref ClipName);
            serializer.SerializeValue(ref IsPlaying);
            serializer.SerializeValue(ref Loop);
            serializer.SerializeValue(ref BlendIn);
            serializer.SerializeValue(ref BlendOut);
            serializer.SerializeValue(ref PlaybackSpeed);
            serializer.SerializeValue(ref ApplyRootMotion);
            serializer.SerializeValue(ref Body);
        }

        public static AnimationStatePayload From(AnimationData animationData, bool isPlaying, float fallbackBlendOut)
        {
            AnimationStatePayload payload = default;
            if (animationData != null && animationData.Clip != null)
            {
                payload.ClipHash = Animator.StringToHash(animationData.Clip.name);
                payload.ClipName = animationData.Clip.name;
            }

            payload.IsPlaying = isPlaying;
            payload.Loop = animationData != null && animationData.Loop;
            payload.BlendIn = animationData != null ? animationData.BlendInDuration : 0f;
            payload.BlendOut = animationData != null ? animationData.BlendOutDuration : fallbackBlendOut;
            payload.PlaybackSpeed = animationData != null ? animationData.PlaybackSpeed : 1f;
            payload.ApplyRootMotion = animationData != null && animationData.ApplyRootMotion;
            payload.Body = animationData != null ? animationData.Body : AbilityAnimationBody.UpperBody;
            return payload;
        }
    }
    #endregion
}
