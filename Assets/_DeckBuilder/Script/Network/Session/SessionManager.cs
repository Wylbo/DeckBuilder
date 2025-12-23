using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    #region Fields
    [SerializeField] private int maxPlayers = 5;
    [SerializeField] private bool isPrivate;
    [SerializeField] private bool isLocked;
    public event Action<string> StatusChanged;
    public event Action<string> JoinCodeChanged;
    public event Action<IReadOnlyList<string>> PlayersChanged;
    public event Action<string> GameplaySceneChanged;
    #endregion

    #region Private Members
    private static SessionManager instance;
    private ISession activeSession;
    private bool isInitializingServices;
    private bool hasInitializedServices;
    private const string PLAYER_NAME_PROPERTY_KEY = "PlayerName";
    private string lastReceivedGameplayScene = string.Empty;
    #endregion

    #region Getters
    public static SessionManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject managerObject = new GameObject("SessionManager");
                instance = managerObject.AddComponent<SessionManager>();
                DontDestroyOnLoad(managerObject);
            }

            return instance;
        }
    }

    public ISession ActiveSession => activeSession;
    public bool HasActiveSession => activeSession != null && activeSession.State != SessionState.None && activeSession.State != SessionState.Disconnected;
    public string CurrentJoinCode => activeSession != null ? activeSession.Code : string.Empty;
    public IReadOnlyList<string> CurrentPlayerIds => GetPlayerIds();
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        InitializeSingleton();
    }

    private void OnDestroy()
    {
        UnsubscribeFromSessionEvents();
    }
    #endregion

    #region Public Methods
    public async Task InitializeServicesAsync()
    {
        if (hasInitializedServices || isInitializingServices)
            return;

        isInitializingServices = true;

        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Unity Services initialization reported: {exception.Message}");
        }

        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            hasInitializedServices = true;
            RaiseStatus("Signed in.");
        }
        catch (Exception exception)
        {
            RaiseStatus($"Sign-in failed: {exception.Message}");
            Debug.LogError($"Failed to initialize Unity Services or sign in: {exception.Message}");
        }
        finally
        {
            isInitializingServices = false;
        }
    }

    public async Task CreateSessionAsHostAsync()
    {
        if (!await EnsureInitializedAsync())
            return;

        RaiseStatus("Creating lobby...");
        try
        {
            Dictionary<string, PlayerProperty> playerProperties = await GetPlayerPropertiesAsync();
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate,
                IsLocked = isLocked,
                PlayerProperties = playerProperties
            }.WithRelayNetwork();

            ISession session = await MultiplayerService.Instance.CreateSessionAsync(options);
            SetActiveSession(session);
            RaiseStatus("Lobby created.");
        }
        catch (Exception exception)
        {
            RaiseStatus($"Failed to create lobby: {exception.Message}");
            Debug.LogError($"Failed to create lobby: {exception}");
        }
    }

    public async Task JoinSessionByCodeAsync(string joinCode)
    {
        if (!await EnsureInitializedAsync())
            return;

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            RaiseStatus("Enter a join code first.");
            return;
        }

        RaiseStatus("Joining lobby...");
        try
        {
            Dictionary<string, PlayerProperty> playerProperties = await GetPlayerPropertiesAsync();
            JoinSessionOptions joinOptions = new JoinSessionOptions
            {
                PlayerProperties = playerProperties
            };

            ISession session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode, joinOptions);
            SetActiveSession(session);
            RaiseStatus("Joined lobby.");
        }
        catch (Exception exception)
        {
            RaiseStatus($"Failed to join lobby: {exception.Message}");
            Debug.LogError($"Failed to join lobby: {exception}");
        }
    }

    public async Task LeaveSessionAsync()
    {
        if (activeSession == null)
            return;

        UnsubscribeFromSessionEvents();

        try
        {
            await activeSession.LeaveAsync();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Error leaving session: {exception.Message}");
        }
        finally
        {
            activeSession = null;
            RaiseJoinCode(string.Empty);
            RaisePlayersChanged(Array.Empty<string>());
            RaiseStatus("Left lobby.");
            lastReceivedGameplayScene = string.Empty;
        }
    }

    public IReadOnlyList<string> GetPlayerNames(IReadOnlyList<string> playerIds)
    {
        if (activeSession == null || activeSession.Players == null || playerIds == null)
            return Array.Empty<string>();

        Dictionary<string, string> playerNamesById = BuildPlayerNameLookup();
        List<string> playerNames = new List<string>(playerIds.Count);
        for (int i = 0; i < playerIds.Count; i++)
        {
            string playerId = playerIds[i];
            if (string.IsNullOrWhiteSpace(playerId))
                continue;

            string playerName;
            if (!playerNamesById.TryGetValue(playerId, out playerName) || string.IsNullOrWhiteSpace(playerName))
                playerName = playerId;

            playerNames.Add(playerName);
        }

        return playerNames;
    }

    public async Task<bool> LoadScene(string sceneName)
    {
        if (activeSession == null || !activeSession.IsHost)
        {
            RaiseStatus("Only the host can start the game.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            RaiseStatus("Invalid gameplay scene name.");
            return false;
        }

        try
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
            return true;
        }
        catch (Exception exception)
        {
            RaiseStatus($"Failed to set gameplay scene: {exception.Message}");
            Debug.LogError($"Failed to save gameplay scene property: {exception}");
            return false;
        }
    }
    #endregion

    #region Private Methods
    private void InitializeSingleton()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async Task<bool> EnsureInitializedAsync()
    {
        await InitializeServicesAsync();
        return hasInitializedServices;
    }

    private async Task<Dictionary<string, PlayerProperty>> GetPlayerPropertiesAsync()
    {
        string playerName = string.Empty;

        try
        {
            playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        }
        catch (Exception)
        {
            playerName = AuthenticationService.Instance.PlayerId;
        }

        PlayerProperty playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
        Dictionary<string, PlayerProperty> properties = new Dictionary<string, PlayerProperty>
        {
            { PLAYER_NAME_PROPERTY_KEY, playerNameProperty }
        };

        return properties;
    }

    private void SetActiveSession(ISession session)
    {
        if (session == null)
            return;

        if (activeSession == session)
            return;

        UnsubscribeFromSessionEvents();
        activeSession = session;
        lastReceivedGameplayScene = string.Empty;
        SubscribeToSessionEvents();

        RaiseJoinCode(activeSession.Code);
        RaisePlayersChanged(GetPlayerIds());
        HandleSessionPropertiesChanged();
    }

    private Dictionary<string, string> BuildPlayerNameLookup()
    {
        Dictionary<string, string> playerNames = new Dictionary<string, string>();
        if (activeSession == null || activeSession.Players == null)
            return playerNames;

        for (int i = 0; i < activeSession.Players.Count; i++)
        {
            IReadOnlyPlayer player = activeSession.Players[i];
            if (player == null || player.Properties == null || string.IsNullOrWhiteSpace(player.Id))
                continue;

            PlayerProperty playerNameProperty;
            if (!player.Properties.TryGetValue(PLAYER_NAME_PROPERTY_KEY, out playerNameProperty))
                continue;

            if (!string.IsNullOrWhiteSpace(playerNameProperty.Value))
                playerNames[player.Id] = playerNameProperty.Value;
        }

        return playerNames;
    }

    private IReadOnlyList<string> GetPlayerIds()
    {
        if (activeSession == null || activeSession.Players == null)
            return Array.Empty<string>();

        List<string> players = new List<string>();
        for (int i = 0; i < activeSession.Players.Count; i++)
        {
            IReadOnlyPlayer player = activeSession.Players[i];
            if (player != null && !string.IsNullOrWhiteSpace(player.Id))
                players.Add(player.Id);
        }

        return players;
    }

    private void SubscribeToSessionEvents()
    {
        if (activeSession == null)
            return;

        activeSession.PlayerJoined += HandlePlayerJoined;
        activeSession.PlayerHasLeft += HandlePlayerHasLeft;
        activeSession.Changed += HandleSessionChanged;
        activeSession.SessionPropertiesChanged += HandleSessionPropertiesChanged;
    }

    private void UnsubscribeFromSessionEvents()
    {
        if (activeSession == null)
            return;

        activeSession.PlayerJoined -= HandlePlayerJoined;
        activeSession.PlayerHasLeft -= HandlePlayerHasLeft;
        activeSession.Changed -= HandleSessionChanged;
        activeSession.SessionPropertiesChanged -= HandleSessionPropertiesChanged;
    }

    private void HandleSessionChanged()
    {
        RaisePlayersChanged(GetPlayerIds());
        RaiseJoinCode(activeSession != null ? activeSession.Code : string.Empty);
        HandleSessionPropertiesChanged();
    }

    private void HandlePlayerJoined(string playerId)
    {
        RaisePlayersChanged(GetPlayerIds());
        RaiseStatus($"Player joined: {playerId}");
    }

    private void HandlePlayerHasLeft(string playerId)
    {
        RaisePlayersChanged(GetPlayerIds());
        RaiseStatus($"Player left: {playerId}");
    }

    private void HandleSessionPropertiesChanged()
    {

    }

    private void RaiseStatus(string message)
    {
        if (StatusChanged != null)
            StatusChanged.Invoke(message);
    }

    private void RaiseJoinCode(string code)
    {
        if (JoinCodeChanged != null)
            JoinCodeChanged.Invoke(code);
    }

    private void RaisePlayersChanged(IReadOnlyList<string> players)
    {
        if (PlayersChanged != null)
            PlayersChanged.Invoke(players);
    }

    #endregion
}
