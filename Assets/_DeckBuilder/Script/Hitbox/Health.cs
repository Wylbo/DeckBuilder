using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth;

    private int CurrentHealth;

    public event UnityAction On_Empty;

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        CurrentHealth = maxHealth;
    }

    public void AddOrRemoveHealth(int damage)
    {
        CurrentHealth += damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
        Debug.Log($"{name} new health:" + CurrentHealth);
        if (CurrentHealth <= 0)
            On_Empty?.Invoke();
    }
}