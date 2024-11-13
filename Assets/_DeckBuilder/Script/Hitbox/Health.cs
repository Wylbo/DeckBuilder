using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth;

    private int currentHealth;

    public int MaxHealth => maxHealth;
    public int Value => currentHealth;

    public event UnityAction On_Empty;

    /// <summary>
    /// send prev and new values
    /// </summary>
    public event UnityAction<int, int> On_Change;

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        currentHealth = maxHealth;
        On_Change?.Invoke(currentHealth, currentHealth);
    }

    public void AddOrRemoveHealth(int damage)
    {
        int previousValue = currentHealth;
        currentHealth += damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        On_Change?.Invoke(previousValue, currentHealth);


        if (currentHealth <= 0)
            On_Empty?.Invoke();
    }
}