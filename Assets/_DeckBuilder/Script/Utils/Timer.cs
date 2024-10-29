using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Timer
{
	[SerializeField]
	private float duration;
	[SerializeField]
	private UnityEvent OnStarted;
	[SerializeField]
	private UnityEvent OnEnded;

	public event UnityAction On_Started;
	public event UnityAction On_Ended;
	public event UnityAction On_Canceled;

	private float remaining = 0;

	public float Remaining => remaining;
	public float ElapsedTime => duration - remaining;
	public float ElapsedRatio => ElapsedTime / duration;
	public bool IsRunning => Remaining > 0;

	public Timer()
	{
		duration = 0f;
	}

	public Timer(float duration)
	{
		this.duration = duration;
	}

	public Timer(Timer timer)
	{
		duration = timer.duration;
	}

	public void Start()
	{
		Start(duration);
	}

	public void Start(float duration)
	{
		if (IsRunning)
			return;

		remaining = duration;

		On_Started?.Invoke();
	}

	public void Update(float deltaTime)
	{
		if (!IsRunning)
			return;

		remaining -= deltaTime;

		if (remaining <= 0)
			Stop();

	}

	public void Cancel()
	{
		remaining = 0f;
		On_Canceled?.Invoke();
	}

	public void Stop()
	{
		remaining = 0;
		On_Ended?.Invoke();
	}
}
