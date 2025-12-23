using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class LocalPlayerCameraBinder : MonoBehaviour
{
    #region Fields
    [Header("References")]
    [SerializeField] private CinemachineCamera virtualCamera;
    #endregion

    #region Private Members
    private Coroutine _bindRoutine;
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        CacheVirtualCamera();
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
    private void CacheVirtualCamera()
    {
        if (virtualCamera == null)
        {
            virtualCamera = GetComponent<CinemachineCamera>();
        }
    }

    private void ValidateDependencies()
    {
        if (virtualCamera != null)
        {
            return;
        }

        Debug.LogError($"{nameof(LocalPlayerCameraBinder)} requires a {nameof(CinemachineCamera)} reference.", this);
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

        Transform playerTransform = playerObject.transform;
        virtualCamera.Follow = playerTransform;
        return true;
    }

    private bool IsNetworkClientReady()
    {
        if (virtualCamera == null)
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
