using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LobbyMenuView : UIView
{
    #region Fields
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusLabel;
    #endregion

    #region Private Members
    #endregion

    #region Getters
    public Button CreateLobbyButton => createLobbyButton;
    public Button JoinLobbyButton => joinLobbyButton;
    public string JoinCodeInput => joinCodeInput != null ? joinCodeInput.text : string.Empty;
    #endregion

    #region Unity Message Methods
    private void OnDisable()
    {
        ClearStatus();
    }
    #endregion

    #region Public Methods
    public void BindCreateLobbyAction(UnityAction action)
    {
        BindButton(createLobbyButton, action);
    }

    public void BindJoinLobbyAction(UnityAction action)
    {
        BindButton(joinLobbyButton, action);
    }

    public void SetStatus(string message)
    {
        if (statusLabel != null)
            statusLabel.text = message ?? string.Empty;
    }

    public void ClearStatus()
    {
        SetStatus(string.Empty);
    }
    #endregion

    #region Private Methods
    private void BindButton(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(action);
    }
    #endregion
}
