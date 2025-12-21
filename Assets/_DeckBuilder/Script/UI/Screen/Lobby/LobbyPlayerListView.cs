using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class LobbyPlayerListView : UIView
{
    #region Fields
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LobbyPlayerListItem playerItemPrefab;
    [SerializeField] private Button copyCodeButton;
    [SerializeField] private TMP_Text joinCodeLabel;
    [SerializeField] private Button leaveLobbyButton;
    #endregion

    #region Private Members
    private readonly Dictionary<string, LobbyPlayerListItem> spawnedItems = new Dictionary<string, LobbyPlayerListItem>();
    #endregion

    #region Getters
    #endregion

    #region Unity Message Methods
    private void OnDisable()
    {
        ClearItems();
        SetJoinCode(string.Empty);
    }
    #endregion

    #region Public Methods
    public void BindCopyCodeAction(UnityAction action)
    {
        BindButton(copyCodeButton, action);
        UpdateCopyButtonState(joinCodeLabel != null ? joinCodeLabel.text : string.Empty);
    }

    public void SetJoinCode(string code)
    {
        string value = string.IsNullOrWhiteSpace(code) ? string.Empty : code;
        if (joinCodeLabel != null)
            joinCodeLabel.text = value;

        UpdateCopyButtonState(value);
    }

    public void ShowPlayers(IEnumerable<string> playerIds)
    {
        if (playerItemPrefab == null || contentRoot == null)
            return;

        if (playerIds == null)
        {
            ClearItems();
            return;
        }

        HashSet<string> seenPlayers = new HashSet<string>();
        foreach (string playerId in playerIds)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                continue;

            seenPlayers.Add(playerId);
            LobbyPlayerListItem item = GetOrCreateItem(playerId);
            if (item != null)
                item.SetPlayerName(playerId);
        }

        RemoveMissingItems(seenPlayers);
    }

    public void SetReadyState(string playerId, bool ready)
    {
        LobbyPlayerListItem item;
        if (!spawnedItems.TryGetValue(playerId, out item))
            return;

        item.SetReadyState(ready);
    }

    public void BindLeaveLobbyAction(UnityAction action)
    {
        BindButton(leaveLobbyButton, action);
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

    private void UpdateCopyButtonState(string code)
    {
        if (copyCodeButton == null)
            return;

        bool hasCode = !string.IsNullOrWhiteSpace(code);
        copyCodeButton.interactable = hasCode;
    }

    private LobbyPlayerListItem GetOrCreateItem(string playerId)
    {
        LobbyPlayerListItem existingItem;
        if (spawnedItems.TryGetValue(playerId, out existingItem))
            return existingItem;

        LobbyPlayerListItem newItem = Instantiate(playerItemPrefab, contentRoot);
        spawnedItems.Add(playerId, newItem);
        return newItem;
    }

    private void RemoveMissingItems(HashSet<string> seenPlayers)
    {
        List<string> playersToRemove = new List<string>();
        foreach (KeyValuePair<string, LobbyPlayerListItem> pair in spawnedItems)
        {
            if (!seenPlayers.Contains(pair.Key))
                playersToRemove.Add(pair.Key);
        }

        for (int i = 0; i < playersToRemove.Count; i++)
        {
            string playerId = playersToRemove[i];
            LobbyPlayerListItem item = spawnedItems[playerId];
            spawnedItems.Remove(playerId);
            if (item != null)
                Destroy(item.gameObject);
        }
    }

    private void ClearItems()
    {
        foreach (KeyValuePair<string, LobbyPlayerListItem> pair in spawnedItems)
        {
            LobbyPlayerListItem item = pair.Value;
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }
    #endregion
}
