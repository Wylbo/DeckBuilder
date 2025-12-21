using System.Collections.Generic;
using UnityEngine;

public class LobbyPresenter : MonoBehaviour
{
    #region Fields
    [SerializeField] private UIManager uiManager;
    [SerializeField] private LobbyMenuView lobbyMenuView;
    [SerializeField] private LobbyPlayerListView lobbyPlayerListView;
    [SerializeField] private SessionManager sessionManager;
    #endregion

    #region Private Members
    private string lastGeneratedJoinCode = string.Empty;
    private bool hasValidDependencies;
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    private void Awake()
    {
        ResolveSessionManager();
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

        if (!hasUiManager || !hasSessionManager)
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
        }
    }

    private void SubscribeToSessionManagerEvents()
    {
        if (sessionManager == null)
            return;

        sessionManager.StatusChanged += HandleStatusUpdated;
        sessionManager.JoinCodeChanged += HandleJoinCodeGenerated;
        sessionManager.PlayersChanged += HandlePlayersChanged;
    }

    private void UnsubscribeFromSessionManagerEvents()
    {
        if (sessionManager == null)
            return;

        sessionManager.StatusChanged -= HandleStatusUpdated;
        sessionManager.JoinCodeChanged -= HandleJoinCodeGenerated;
        sessionManager.PlayersChanged -= HandlePlayersChanged;
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
            return;
        }

        ShowMenuScreen();
        ResetJoinCodeLabel();
    }
    #endregion
}
