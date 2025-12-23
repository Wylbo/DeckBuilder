using System;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Health : NetworkBehaviour
{
    [SerializeField] private int maxHealth;

    [ShowInInspector, ProgressBar(0, "maxHealth")]
    private int currentHealth;

    private NetworkVariable<int> networkCurrentHealth = new NetworkVariable<int>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server
        );

    public int MaxHealth => maxHealth;
    public int Value => currentHealth;

    public event UnityAction On_Empty;

    /// <summary>
    /// send prev and new values
    /// </summary>
    public event UnityAction<int, int> On_Change;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    public void Initialize()
    {
        networkCurrentHealth.OnValueChanged += NetworkCurrentHealth_OnValueChanged;
        if (IsServer)
            networkCurrentHealth.Value = maxHealth;

        currentHealth = networkCurrentHealth.Value;
        On_Change?.Invoke(currentHealth, currentHealth);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkCurrentHealth.OnValueChanged -= NetworkCurrentHealth_OnValueChanged;
    }

    public void AddOrRemoveHealth(int damage)
    {
        UpdateHealth_ServerRpc(damage);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void UpdateHealth_ServerRpc(int delta)
    {
        int newValue = Mathf.Clamp(networkCurrentHealth.Value + delta, 0, maxHealth);
        networkCurrentHealth.Value = newValue;
    }

    private void NetworkCurrentHealth_OnValueChanged(int previousValue, int newValue)
    {
        currentHealth = newValue;
        On_Change?.Invoke(previousValue, currentHealth);

        if (currentHealth <= 0)
            On_Empty?.Invoke();
    }
}