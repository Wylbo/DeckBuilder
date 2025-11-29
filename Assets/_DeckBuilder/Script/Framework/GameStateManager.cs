using System;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public enum GameState
    {
        Playing,
        Paused
    }

    [SerializeField] private bool useTimeScale = true;

    private readonly HashSet<object> pauseRequesters = new HashSet<object>();
    private GameState currentState = GameState.Playing;

    public GameState CurrentState => currentState;
    public bool IsPaused => currentState == GameState.Paused;

    public event Action<GameState> OnGameStateChanged;

    private void Start()
    {
        ApplyState(currentState, false, true);
    }

    public void RequestPause(object source)
    {
        if (source == null)
        {
            return;
        }

        if (pauseRequesters.Add(source))
        {
            ApplyState(GameState.Paused);
        }
    }

    public void ReleasePause(object source)
    {
        if (source == null)
        {
            return;
        }

        if (!pauseRequesters.Remove(source))
        {
            return;
        }

        if (pauseRequesters.Count == 0)
        {
            ApplyState(GameState.Playing);
        }
    }

    public void ForceState(GameState targetState)
    {
        pauseRequesters.Clear();

        if (targetState == GameState.Paused)
        {
            pauseRequesters.Add(this);
        }

        ApplyState(targetState);
    }

    private void ApplyState(GameState targetState, bool invokeEvent = true, bool forceUpdate = false)
    {
        if (currentState == targetState && !forceUpdate)
        {
            return;
        }

        currentState = targetState;

        if (useTimeScale)
        {
            Time.timeScale = currentState == GameState.Paused ? 0f : 1f;
        }

        if (invokeEvent)
        {
            OnGameStateChanged?.Invoke(currentState);
        }
    }
}
