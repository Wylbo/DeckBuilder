using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[Serializable]
public struct CameraShakeData
{
    #region Fields
    [SerializeField] private float duration;
    [SerializeField] private NoiseSettings noiseSettings;
    [SerializeField] private Vector3 pivotOffset;
    [SerializeField] private float amplitudeGain;
    [SerializeField] private AnimationCurve amplitudeOverTime;
    [SerializeField] private float frequencyGain;
    [SerializeField] private AnimationCurve frequencyOverTime;
    #endregion

    #region Private Members
    #endregion

    #region Getters
    public float Duration => duration;
    public NoiseSettings NoiseSettings => noiseSettings;
    public Vector3 PivotOffset => pivotOffset;
    public float AmplitudeGain => amplitudeGain;
    public AnimationCurve AmplitudeOverTime => amplitudeOverTime;
    public float FrequencyGain => frequencyGain;
    public AnimationCurve FrequencyOverTime => frequencyOverTime;
    #endregion

    #region Unity Message Methods
    #endregion

    #region Public Methods
    #endregion

    #region Private Methods
    #endregion
}

public class CameraEffectManager : MonoBehaviour
{
    #region Fields
    [SerializeField] private CinemachineBrain cineBrain;
    [SerializeField] private CameraShakeData defaultShakeData;
    #endregion

    #region Private Members
    private CinemachineCamera activeCamera;
    private CinemachineBasicMultiChannelPerlin noiseComponent;
    private Coroutine activeShakeRoutine;
    #endregion

    #region Getters
    public CinemachineCamera ActiveCamera => activeCamera;
    public CinemachineBasicMultiChannelPerlin NoiseComponent => noiseComponent;
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        ValidateSerializedReferences();
        InitializeCameraEffects();
    }

    private void OnValidate()
    {
        ValidateSerializedReferences();
    }
    #endregion

    #region Public Methods
    public void ScreenShake()
    {
        ScreenShake(defaultShakeData);
    }

    public void ScreenShake(CameraShakeData shakeData)
    {
        if (!TryPrepareNoiseComponent())
        {
            return;
        }

        if (activeShakeRoutine != null)
        {
            StopCoroutine(activeShakeRoutine);
        }

        activeShakeRoutine = StartCoroutine(ShakeRoutine(shakeData));
    }
    #endregion

    #region Private Methods
    private void ValidateSerializedReferences()
    {
        if (cineBrain == null)
        {
            cineBrain = GetComponent<CinemachineBrain>();
        }
    }

    private void InitializeCameraEffects()
    {
        if (cineBrain == null)
        {
            Debug.LogError($"{nameof(CameraEffectManager)} requires a {nameof(CinemachineBrain)} reference.", this);
            enabled = false;
            return;
        }

        UpdateActiveCameraReferences();
    }

    private bool TryPrepareNoiseComponent()
    {
        if (cineBrain == null)
        {
            Debug.LogError($"{nameof(CameraEffectManager)} requires a {nameof(CinemachineBrain)} reference.", this);
            return false;
        }

        UpdateActiveCameraReferences();

        if (activeCamera == null)
        {
            Debug.LogWarning("No active CinemachineCamera found for screen shake.", this);
            return false;
        }

        if (noiseComponent == null)
        {
            Debug.LogWarning("No CinemachineBasicMultiChannelPerlin component found on the active camera.", this);
            return false;
        }

        return true;
    }

    private void UpdateActiveCameraReferences()
    {
        activeCamera = cineBrain.ActiveVirtualCamera as CinemachineCamera;
        noiseComponent = activeCamera != null ? activeCamera.GetComponent<CinemachineBasicMultiChannelPerlin>() : null;
    }

    private IEnumerator ShakeRoutine(CameraShakeData shakeData)
    {
        ResetNoise();

        float elapsedTime = 0f;

        while (elapsedTime < shakeData.Duration)
        {
            float normalizedTime = elapsedTime / shakeData.Duration;
            elapsedTime += Time.deltaTime;

            float amplitudeRatio = shakeData.AmplitudeOverTime.Evaluate(normalizedTime);
            float frequencyRatio = shakeData.FrequencyOverTime.Evaluate(normalizedTime);

            noiseComponent.AmplitudeGain = shakeData.AmplitudeGain * amplitudeRatio;
            noiseComponent.FrequencyGain = shakeData.FrequencyGain * frequencyRatio;

            yield return null;
        }

        ResetNoise();
    }

    private void ResetNoise()
    {
        if (noiseComponent == null)
        {
            return;
        }

        noiseComponent.AmplitudeGain = 0f;
        noiseComponent.FrequencyGain = 0f;
    }
    #endregion
}
