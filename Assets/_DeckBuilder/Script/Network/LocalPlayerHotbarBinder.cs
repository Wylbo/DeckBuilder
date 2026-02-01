using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Binds the Hotbar UI to the local player's AbilityCaster when the player spawns.
/// Uses a polling coroutine to wait for the local player to become available.
/// </summary>
[DisallowMultipleComponent]
public class LocalPlayerHotbarBinder : MonoBehaviour
{
    #region Fields
    [Header("References")]
    [SerializeField]
    [Tooltip("The Hotbar component to bind to the local player's AbilityCaster")]
    private Hotbar hotbar;
    #endregion

    #region Private Members
    private Coroutine _bindRoutine;
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        ValidateDependencies();
    }

    private void OnEnable()
    {
        SubscribeToClientConnected();
        StartBindingRoutine();
    }

    private void OnDisable()
    {
        UnsubscribeFromClientConnected();
        StopBindingRoutine();
    }
    #endregion

    #region Public Methods
    #endregion

    #region Private Methods
    private void ValidateDependencies()
    {
        if (hotbar != null)
        {
            return;
        }

        Debug.LogError($"{nameof(LocalPlayerHotbarBinder)} requires a {nameof(Hotbar)} reference.", this);
        enabled = false;
    }

    private void SubscribeToClientConnected()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void UnsubscribeFromClientConnected()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong _)
    {
        StartBindingRoutine();
    }

    private void StartBindingRoutine()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        StopBindingRoutine();
        _bindRoutine = StartCoroutine(BindWhenLocalPlayerAvailable());
    }

    private void StopBindingRoutine()
    {
        if (_bindRoutine == null)
        {
            return;
        }

        StopCoroutine(_bindRoutine);
        _bindRoutine = null;
    }

    private IEnumerator BindWhenLocalPlayerAvailable()
    {
        while (enabled)
        {
            if (TryBindToLocalPlayer())
            {
                _bindRoutine = null;
                yield break;
            }

            yield return null;
        }

        _bindRoutine = null;
    }

    private bool TryBindToLocalPlayer()
    {
        if (!IsNetworkClientReady())
        {
            return false;
        }

        NetworkClient localClient = NetworkManager.Singleton.LocalClient;
        if (localClient == null)
        {
            return false;
        }

        NetworkObject playerObject = localClient.PlayerObject;
        if (playerObject == null)
        {
            return false;
        }

        AbilityCaster abilityCaster = playerObject.GetComponentInChildren<AbilityCaster>();
        if (abilityCaster == null)
        {
            Debug.LogWarning($"{nameof(LocalPlayerHotbarBinder)}: Local player does not have an {nameof(AbilityCaster)} component.", this);
            return false;
        }

        hotbar.SetCaster(abilityCaster);
        return true;
    }

    private bool IsNetworkClientReady()
    {
        if (hotbar == null)
        {
            return false;
        }

        if (NetworkManager.Singleton == null)
        {
            return false;
        }

        return NetworkManager.Singleton.IsClient;
    }
    #endregion
}
