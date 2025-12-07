using System;
using UnityEngine;

/// <summary>
/// Authoring data for an ability animation clip played through the Playables graph.
/// </summary>
[Serializable]
public sealed class AbilityAnimationData
{
    [SerializeField] private AnimationClip clip = null;
    [SerializeField] private AbilityAnimationBody body = AbilityAnimationBody.UpperBody;
    [SerializeField, Min(0f)] private float blendInDuration = 0.1f;
    [SerializeField, Min(0f)] private float blendOutDuration = 0.1f;
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool applyRootMotion = false;
    [SerializeField, Tooltip("If true, the ability will stay in casting state until the clip duration elapses.")]
    private bool blockAbilityEndForClip = true;

    public AnimationClip Clip => clip;
    public AbilityAnimationBody Body => body;
    public float BlendInDuration => blendInDuration;
    public float BlendOutDuration => blendOutDuration;
    public float PlaybackSpeed => playbackSpeed;
    public bool Loop => loop;
    public bool ApplyRootMotion => applyRootMotion;
    public bool BlockAbilityEndForClip => blockAbilityEndForClip;

    public float GetEffectiveDurationSeconds()
    {
        if (clip == null)
            return 0f;

        if (loop)
            return float.PositiveInfinity;

        float speed = Mathf.Approximately(playbackSpeed, 0f) ? 1f : Mathf.Abs(playbackSpeed);
        return clip.length / speed;
    }
}

public enum AbilityAnimationBody
{
    FullBody = 0,
    UpperBody = 1
}
