using UnityEngine;
using System;
using System.Collections;
using Unity.Cinemachine;

[Serializable]
public struct CameraShakeData
{
    [SerializeField] private float duration;
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

    public CinemachineCamera ActiveCam => cineBrain.ActiveVirtualCamera as CinemachineCamera;
    public CinemachineBasicMultiChannelPerlin noise => ActiveCam.GetComponent<CinemachineBasicMultiChannelPerlin>();

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

        noise.NoiseProfile = shakeData.NoiseSettings;
        noise.PivotOffset = shakeData.PivotOffset;
        noise.AmplitudeGain = 0;
        noise.FrequencyGain = 0;

        while (elapsed < shakeData.Duration)
        {
            normalizedTime = elapsed / shakeData.Duration;
            elapsed += Time.deltaTime;

            curveValueAmp = shakeData.AmplitudeOverTime.Evaluate(normalizedTime);
            curveValueFreq = shakeData.FrequencyOverTime.Evaluate(normalizedTime);

            noise.AmplitudeGain = shakeData.AmplitudeGain * curveValueAmp;
            noise.FrequencyGain = shakeData.FrequencyGain * curveValueFreq;

            yield return null;
        }

        noise.AmplitudeGain = 0;
        noise.FrequencyGain = 0;
    }
}
