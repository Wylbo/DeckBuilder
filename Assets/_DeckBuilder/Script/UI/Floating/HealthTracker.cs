using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HealthTracker : MonoBehaviour
{
    [SerializeField] private Health HealthToTrack;
    [SerializeField] private Image foreground;
    [SerializeField] private Image background;
    [SerializeField] private Color increaseHealthColor;
    [SerializeField] private Color decreaseHealthColor;
    [SerializeField] private float decreaseDuration;
    [SerializeField] private float increaseDuration;
    [SerializeField] private AnimationCurve fillCurve;

    private void OnEnable()
    {
        HealthToTrack.On_Change += HealToTrack_On_Change;
    }

    private void HealToTrack_On_Change(int prevVal, int newVal)
    {
        if (prevVal - newVal > 0)
            Decrease(newVal);
        else
            Increase(newVal);
    }

    private void Increase(float newVal)
    {
        background.fillAmount = newVal / HealthToTrack.MaxHealth;
        background.color = increaseHealthColor;
        foreground.DOFillAmount(newVal / HealthToTrack.MaxHealth, increaseDuration).SetEase(fillCurve);
    }

    private void Decrease(float newVal)
    {
        foreground.fillAmount = newVal / HealthToTrack.MaxHealth;
        background.color = decreaseHealthColor;
        background.DOFillAmount(newVal / HealthToTrack.MaxHealth, increaseDuration).SetEase(fillCurve);
    }
}
