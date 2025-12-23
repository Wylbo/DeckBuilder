using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class allowing to control a character.
/// Supports player control on the owning client and optional server authority for AI.
/// </summary>
[RequireComponent(typeof(Character))]
public class Controller : NetworkBehaviour
{
    #region Fields
    [SerializeField] private Character character;
    [SerializeField] private ControlStrategy controlStrategy;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool allowServerAuthority = true;
    #endregion

    #region Private Members
    private ControlStrategy runtimeControlStrategy;
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    public override void OnNetworkSpawn()
    {
        InitializeControlStrategy();
    }

    public override void OnNetworkDespawn()
    {
        DisableControlStrategy();
    }

    public override void OnGainedOwnership()
    {
        InitializeControlStrategy();
    }

    public override void OnLostOwnership()
    {
        DisableControlStrategy();
    }

    protected virtual void Update()
    {
        if (!HasControlAuthority())
        {
            if (runtimeControlStrategy != null)
                DisableControlStrategy();
            return;
        }

        runtimeControlStrategy?.Control(Time.deltaTime);
    }

    public override void OnDestroy()
    {
        DisableControlStrategy();
    }

    private void Reset()
    {
        character = GetComponent<Character>();
    }
    #endregion

    #region Public Methods
    public bool TryMove(Vector3 worldTo)
    {
        if (!HasControlAuthority() || runtimeControlStrategy == null || !CanControlCharacter())
            return false;

        return character.MoveTo(worldTo);
    }

    public void CastAbility(int index, Vector3 worldPos)
    {
        if (!HasControlAuthority() || !CanControlCharacter())
            return;

        character.CastAbility(index, worldPos);
    }

    public void EndHold(int index, Vector3 worldPos)
    {
        if (!HasControlAuthority() || !CanControlCharacter())
            return;

        character.EndHold(index, worldPos);
    }

    public void PerformDodge(Vector3 worldPos)
    {
        if (!HasControlAuthority() || !CanControlCharacter())
            return;

        character.PerformDodge(worldPos);
    }
    #endregion

    #region Private Methods
    private void InitializeControlStrategy()
    {
        if (!HasControlAuthority())
            return;

        if (runtimeControlStrategy != null)
            return;

        if (controlStrategy == null || character == null)
        {
            Debug.LogError($"[{nameof(Controller)}] Cannot initialize control strategy because required references are missing.", this);
            return;
        }

        runtimeControlStrategy = Instantiate(controlStrategy);
        runtimeControlStrategy.Initialize(this, character, ResolveUIManager());
    }

    private void DisableControlStrategy()
    {
        runtimeControlStrategy?.Disable();
        runtimeControlStrategy = null;
    }

    private IUIManager ResolveUIManager()
    {
        if (uiManager == null && IsOwner)
            uiManager = FindFirstObjectByType<UIManager>();

        return uiManager;
    }

    private bool CanControlCharacter()
    {
        if (character != null)
            return true;

        Debug.LogError($"[{nameof(Controller)}] Cannot forward control because the character reference is missing.", this);
        return false;
    }

    private bool HasControlAuthority()
    {
        if (!IsSpawned)
            return true;

        if (IsOwner)
            return true;

        if (allowServerAuthority && IsServer && NetworkObject != null && NetworkObject.OwnerClientId == NetworkManager.ServerClientId)
            return true;

        return false;
    }
    #endregion
}
