using UnityEngine;
using UnityEngine.Events;

public class TimerEvent : MonoBehaviour
{
    [SerializeField] private Timer timer;
    [SerializeField] private UnityEvent OnTimerStart;
    [SerializeField] private UnityEvent OnTimerEnd;
    [SerializeField] private UnityEvent OnTimerCanceled;

    private void OnEnable()
    {
        timer.On_Started += OnTimerStart.Invoke;
        timer.On_Ended += OnTimerEnd.Invoke;
        timer.On_Canceled += OnTimerCanceled.Invoke;
        timer.Start();
    }

    private void OnDisable()
    {
        timer.Cancel();
        timer.On_Started -= OnTimerStart.Invoke;
        timer.On_Ended -= OnTimerEnd.Invoke;
        timer.On_Canceled -= OnTimerCanceled.Invoke;
    }

    private void Update()
    {
        timer.Update(Time.deltaTime);
    }
}