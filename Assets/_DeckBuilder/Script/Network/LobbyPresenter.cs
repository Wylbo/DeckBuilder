using System.Collections.Generic;
using UnityEngine;

public class LobbyPresenter : MonoBehaviour
{
    #region Fields
    [SerializeField] private UIManager uiManager;
    [SerializeField] private LobbyMenuView lobbyMenuView;
    [SerializeField] private LobbyPlayerListView lobbyPlayerListView;
    [SerializeField] private SessionManager sessionManager;
    [SerializeField] private SceneController sceneController;
    #endregion

    #region Private Members
    private string lastGeneratedJoinCode = string.Empty;
    private bool hasValidDependencies;
    private const string GAMEPLAY_SCENE_NAME = "GameplaySceneTest";
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        ResolveSessionManager();
        ResolveSceneController();
        hasValidDependencies = ValidateDependencies();
        if (!hasValidDependencies)
        {
            enabled = false;
            return;
        }

        InitializeServices();
    }

    private void OnEnable()
    {
        if (!hasValidDependencies)
            return;

        if (!EnsureViewsAvailable())
        {
            Debug.LogError("LobbyPresenter could not obtain required views.", this);
            enabled = false;
            return;
        }

        ShowMenuScreen();
        BindView();
        SubscribeToSessionManagerEvents();
        ResetJoinCodeLabel();
        if (lobbyMenuView != null)
            lobbyMenuView.ClearStatus();

        SyncExistingSessionState();
        UpdateScreenForSessionState();
    }

    private void OnDisable()
    {
        if (!hasValidDependencies)
            return;

        UnbindView();
        UnsubscribeFromSessionManagerEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromSessionManagerEvents();
    }
    #endregion

    #region Public Methods
    #endregion

    #region Private Methods
    private void InitializeServices()
    {
        if (sessionManager != null)
            _ = sessionManager.InitializeServicesAsync();
    }

    private bool ValidateDependencies()
    {
        bool hasUiManager = uiManager != null;
        bool hasSessionManager = sessionManager != null;
        bool hasSceneController = sceneController != null;

        if (!hasUiManager || !hasSessionManager || !hasSceneController)
        {
            Debug.LogError("LobbyPresenter missing required references.", this);
            return false;
        }

        return true;
    }

    private bool EnsureViewsAvailable()
    {
        LobbyMenuView menuView = uiManager.Show<LobbyMenuView>();
        if (menuView != null)
            lobbyMenuView = menuView;

        LobbyPlayerListView rosterView = uiManager.Show<LobbyPlayerListView>();
        if (rosterView != null)
        {
            lobbyPlayerListView = rosterView;
            uiManager.Hide<LobbyPlayerListView>();
        }

        bool hasMenuView = lobbyMenuView != null;
        bool hasRosterView = lobbyPlayerListView != null;
        return hasMenuView && hasRosterView;
    }

    private void BindView()
    {
        if (lobbyMenuView != null)
        {
            lobbyMenuView.BindCreateLobbyAction(HandleCreateLobbyClicked);
            lobbyMenuView.BindJoinLobbyAction(HandleJoinLobbyClicked);
        }

        if (lobbyPlayerListView != null)
        {
            lobbyPlayerListView.BindCopyCodeAction(HandleCopyCodeClicked);
            lobbyPlayerListView.BindLeaveLobbyAction(HandleLeaveLobbyClicked);
            lobbyPlayerListView.BindStartGameAction(HandleStartGameClicked);
        }
    }

    private void UnbindView()
    {
        if (lobbyMenuView != null)
        {
            lobbyMenuView.BindCreateLobbyAction(null);
            lobbyMenuView.BindJoinLobbyAction(null);
        }

        if (lobbyPlayerListView != null)
        {
            lobbyPlayerListView.BindCopyCodeAction(null);
            lobbyPlayerListView.BindLeaveLobbyAction(null);
            lobbyPlayerListView.BindStartGameAction(null);
        }
    }

    private void SubscribeToSessionManagerEvents()
    {
        if (sessionManager == null)
            return;

        sessionManager.StatusChanged += HandleStatusUpdated;
        sessionManager.JoinCodeChanged += HandleJoinCodeGenerated;
        sessionManager.PlayersChanged += HandlePlayersChanged;
        sessionManager.GameplaySceneChanged += HandleGameplaySceneChanged;
    }

    private void UnsubscribeFromSessionManagerEvents()
    {
        if (sessionManager == null)
            return;

        sessionManager.StatusChanged -= HandleStatusUpdated;
        sessionManager.JoinCodeChanged -= HandleJoinCodeGenerated;
        sessionManager.PlayersChanged -= HandlePlayersChanged;
        sessionManager.GameplaySceneChanged -= HandleGameplaySceneChanged;
    }

    private void HandleStatusUpdated(string status)
    {
        if (lobbyMenuView != null)
            lobbyMenuView.SetStatus(status);

        UpdateScreenForSessionState();
    }

    private void HandleJoinCodeGenerated(string code)
    {
        lastGeneratedJoinCode = code;
        if (lobbyPlayerListView != null)
            lobbyPlayerListView.SetJoinCode(code);
    }

    private void HandlePlayersChanged(IReadOnlyList<string> playerIds)
    {
        if (lobbyPlayerListView == null)
            return;

        IReadOnlyList<string> playerNames = sessionManager != null ? sessionManager.GetPlayerNames(playerIds) : playerIds;
        lobbyPlayerListView.ShowPlayers(playerNames);
    }

    private void HandleGameplaySceneChanged(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (sceneController == null)
        {
            Debug.LogError("Cannot load gameplay scene because no SceneController is available.", this);
            return;
        }

        bool loaded = sceneController.LoadScene(sceneName);
        if (!loaded && lobbyMenuView != null)
            lobbyMenuView.SetStatus("Unable to load gameplay scene.");
    }

    private async void HandleCreateLobbyClicked()
    {
        if (sessionManager == null)
            return;

        await sessionManager.CreateSessionAsHostAsync();
        SyncExistingSessionState();
        UpdateScreenForSessionState();
    }

    private async void HandleJoinLobbyClicked()
    {
        if (sessionManager == null)
            return;

        string inputCode = lobbyMenuView != null ? lobbyMenuView.JoinCodeInput : string.Empty;
        await sessionManager.JoinSessionByCodeAsync(inputCode);
        SyncExistingSessionState();
        UpdateScreenForSessionState();
    }

    private async void HandleLeaveLobbyClicked()
    {
        if (sessionManager == null)
            return;

        await sessionManager.LeaveSessionAsync();
        UpdateScreenForSessionState();
    }

    private void HandleCopyCodeClicked()
    {
        if (string.IsNullOrWhiteSpace(lastGeneratedJoinCode))
        {
            if (lobbyMenuView != null)
                lobbyMenuView.SetStatus("No join code available to copy.");
            return;
        }

        GUIUtility.systemCopyBuffer = lastGeneratedJoinCode;
        if (lobbyMenuView != null)
            lobbyMenuView.SetStatus("Join code copied.");
    }

    private async void HandleStartGameClicked()
    {
        if (!ValidateStartGame())
            return;

        bool started = await sessionManager.LoadScene(GAMEPLAY_SCENE_NAME);

        if (!started && lobbyMenuView != null)
            lobbyMenuView.SetStatus("Unable to start gameplay.");
    }

    private bool ValidateStartGame()
    {
        if (sessionManager == null || !sessionManager.HasActiveSession)
        {
            if (lobbyMenuView != null)
                lobbyMenuView.SetStatus("Create or join a lobby before starting the game.");
            return false;
        }

        if (sceneController == null)
        {
            Debug.LogError("Cannot start gameplay because no SceneController is available.", this);
            return false;
        }

        return true;
    }

    private void ResetJoinCodeLabel()
    {
        lastGeneratedJoinCode = string.Empty;
        if (lobbyPlayerListView != null)
            lobbyPlayerListView.SetJoinCode(string.Empty);
    }

    private void ShowMenuScreen()
    {
        LobbyMenuView menuView = uiManager.Show<LobbyMenuView>();
        if (menuView != null)
            lobbyMenuView = menuView;

        uiManager.Hide<LobbyPlayerListView>();
    }

    private void ShowRosterScreen()
    {
        LobbyPlayerListView rosterView = uiManager.Show<LobbyPlayerListView>();
        if (rosterView != null)
            lobbyPlayerListView = rosterView;

        uiManager.Hide<LobbyMenuView>();
    }

    private void ResolveSessionManager()
    {
        if (sessionManager != null)
            return;

        sessionManager = FindFirstObjectByType<SessionManager>();
        if (sessionManager == null)
            sessionManager = SessionManager.Instance;
    }

    private void ResolveSceneController()
    {
        if (sceneController != null)
            return;

        sceneController = SceneController.Instance;
        if (sceneController == null)
            sceneController = FindFirstObjectByType<SceneController>();
    }

    private void SyncExistingSessionState()
    {
        if (sessionManager == null)
            return;

        HandleJoinCodeGenerated(sessionManager.CurrentJoinCode);
        HandlePlayersChanged(sessionManager.CurrentPlayerIds);
    }

    private void UpdateScreenForSessionState()
    {
        if (sessionManager != null && sessionManager.HasActiveSession)
        {
            ShowRosterScreen();
            UpdateStartButtonState(true);
            return;
        }

        ShowMenuScreen();
        ResetJoinCodeLabel();
        UpdateStartButtonState(false);
    }

    private void UpdateStartButtonState(bool hasActiveSession)
    {
        if (lobbyPlayerListView == null)
            return;

        bool isHost = sessionManager != null && sessionManager.ActiveSession != null && sessionManager.ActiveSession.IsHost;
        bool canStart = hasActiveSession && isHost && sceneController != null && !sceneController.IsLoading;
        lobbyPlayerListView.SetStartButtonInteractable(canStart);
    }
    #endregion
}
