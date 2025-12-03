using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    private readonly HashSet<Character> aliveEnemies = new HashSet<Character>();
    private readonly Dictionary<Character, UnityAction> deathHandlers = new Dictionary<Character, UnityAction>();
    private readonly Dictionary<Character, UnityAction<Character>> releaseHandlers = new Dictionary<Character, UnityAction<Character>>();

    public IReadOnlyCollection<Character> AliveEnemies => aliveEnemies;
    public int AliveCount => aliveEnemies.Count;

    public event UnityAction<Character> OnEnemySpawned;
    public event UnityAction<Character> OnEnemyRemoved;

    public Character SpawnFromPool(Character prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        Character instance = PoolManager.Provide<Character>(prefab.gameObject, position, rotation, transform);
        if (instance == null)
        {
            Debug.LogError($"[{nameof(EnemyManager)}] Spawned object from {prefab.name} is missing a {nameof(Character)} component", this);
            return null;
        }

        Register(instance);
        return instance;
    }

    public void Register(Character character)
    {
        if (character == null || aliveEnemies.Contains(character))
        {
            return;
        }

        aliveEnemies.Add(character);

        UnityAction deathHandler = null;
        deathHandler = () => HandleEnemyDeath(character);
        deathHandlers[character] = deathHandler;
        character.On_Died += deathHandler;

        UnityAction<Character> releaseHandler = null;
        releaseHandler = c => HandleEnemyReadyForRelease(c);
        releaseHandlers[character] = releaseHandler;
        character.On_ReadyForRelease += releaseHandler;

        OnEnemySpawned?.Invoke(character);
    }

    public void Unregister(Character character, bool detachReleaseHandler = true)
    {
        if (character == null || !aliveEnemies.Contains(character))
        {
            return;
        }

        if (deathHandlers.TryGetValue(character, out UnityAction deathHandler))
        {
            character.On_Died -= deathHandler;
            deathHandlers.Remove(character);
        }

        if (detachReleaseHandler)
        {
            DetachReleaseHandler(character);
        }

        aliveEnemies.Remove(character);
        OnEnemyRemoved?.Invoke(character);
    }

    public void Clear(bool destroyEnemies)
    {
        Character[] snapshot = new Character[aliveEnemies.Count];
        aliveEnemies.CopyTo(snapshot);

        foreach (Character enemy in snapshot)
        {
            if (destroyEnemies)
            {
                DetachReleaseHandler(enemy);
                Destroy(enemy.gameObject);
            }
            else
            {
                ReleaseCharacter(enemy);
            }

            Unregister(enemy);
        }
    }

    private void HandleEnemyDeath(Character character)
    {
        Unregister(character, detachReleaseHandler: false);
    }

    private void HandleEnemyReadyForRelease(Character character)
    {
        ReleaseCharacter(character);
    }

    private void ReleaseCharacter(Character character)
    {
        if (character == null || character.gameObject == null)
        {
            return;
        }

        DetachReleaseHandler(character);
        PoolManager.Release(character.gameObject);
    }

    private void DetachReleaseHandler(Character character)
    {
        if (character == null)
        {
            return;
        }

        if (releaseHandlers.TryGetValue(character, out UnityAction<Character> releaseHandler))
        {
            character.On_ReadyForRelease -= releaseHandler;
            releaseHandlers.Remove(character);
        }
    }
}
