using TMPro;
using UnityEngine;

public class LobbyPlayerListItem : MonoBehaviour
{
    #region Fields
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private TMP_Text readyStateLabel;
    [SerializeField] private string readyText = "Ready";
    [SerializeField] private string notReadyText = "Not Ready";
    #endregion

    #region Private Members
    private string playerId;
    #endregion

    #region Getters
    public string PlayerId => playerId;
    #endregion

    #region Unity Message Methods
    #endregion

    #region Public Methods
    public void SetPlayerName(string newPlayerId)
    {
        playerId = newPlayerId;
        if (playerNameLabel != null)
            playerNameLabel.text = string.IsNullOrWhiteSpace(newPlayerId) ? "Unknown" : newPlayerId;
    }

    public void SetReadyState(bool isReady)
    {
        if (readyStateLabel != null)
            readyStateLabel.text = isReady ? readyText : notReadyText;
    }
    #endregion

    #region Private Methods
    #endregion
}
