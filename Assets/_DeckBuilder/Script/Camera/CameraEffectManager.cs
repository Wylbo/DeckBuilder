using UnityEngine;
using Cinemachine;
using System;
using System.Collections;

[Serializable]
public struct CameraShakeData
{
    [SerializeField] private float duration;
    [NoiseSettingsProperty]
    [SerializeField] private NoiseSettings noiseSettings;
    [SerializeField] private Vector3 pivotOffset;
    [SerializeField] private float amplitudeGain;
    [SerializeField] private AnimationCurve amplitudeOverTime;
    [SerializeField] private float frequencyGain;
    [SerializeField] private AnimationCurve frequencyOverTime;

    public float Duration => duration;
    public NoiseSettings NoiseSettings => noiseSettings;
    public Vector3 PivotOffset => pivotOffset;
    public float AmplitudeGain => amplitudeGain;
    public AnimationCurve AmplitudeOverTime => amplitudeOverTime;
    public float FrequencyGain => frequencyGain;
    public AnimationCurve FrequencyOverTime => frequencyOverTime;
}

public class CameraEffectManager : MonoBehaviour
{
    [SerializeField] private CinemachineBrain cineBrain;
    [SerializeField] private CameraShakeData defaultShakeData;

    public CinemachineVirtualCamera ActiveCam => cineBrain.ActiveVirtualCamera as CinemachineVirtualCamera;
    public CinemachineBasicMultiChannelPerlin noise => ActiveCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

    public void ScreenShake()
    {
        ScreenShake(defaultShakeData);
    }

    public void ScreenShake(CameraShakeData shakeData)
    {
        StartCoroutine(Shake_Routine(shakeData));
    }

    private IEnumerator Shake_Routine(CameraShakeData shakeData)
    {
        float elapsed = 0;
        float normalizedTime;
        float curveValueFreq;
        float curveValueAmp;

        noise.m_NoiseProfile = shakeData.NoiseSettings;
        noise.m_PivotOffset = shakeData.PivotOffset;
        noise.m_AmplitudeGain = 0;
        noise.m_FrequencyGain = 0;

        while (elapsed < shakeData.Duration)
        {
            normalizedTime = elapsed / shakeData.Duration;
            elapsed += Time.deltaTime;

            curveValueAmp = shakeData.AmplitudeOverTime.Evaluate(normalizedTime);
            curveValueFreq = shakeData.FrequencyOverTime.Evaluate(normalizedTime);

            noise.m_AmplitudeGain = shakeData.AmplitudeGain * curveValueAmp;
            noise.m_FrequencyGain = shakeData.FrequencyGain * curveValueFreq;

            yield return null;
        }

        noise.m_AmplitudeGain = 0;
        noise.m_FrequencyGain = 0;
    }
}
