using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    private readonly HashSet<Character> aliveEnemies = new HashSet<Character>();
    private readonly Dictionary<Character, UnityAction> deathHandlers = new Dictionary<Character, UnityAction>();

    public IReadOnlyCollection<Character> AliveEnemies => aliveEnemies;
    public int AliveCount => aliveEnemies.Count;

    public event UnityAction<Character> OnEnemySpawned;
    public event UnityAction<Character> OnEnemyRemoved;

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

        OnEnemySpawned?.Invoke(character);
    }

    public void Unregister(Character character)
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
                Destroy(enemy.gameObject);
            }
            else
            {
                enemy.gameObject.SetActive(false);
            }

            Unregister(enemy);
        }
    }

    private void HandleEnemyDeath(Character character)
    {
        Unregister(character);
    }
}
