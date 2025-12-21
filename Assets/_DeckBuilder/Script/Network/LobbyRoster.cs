using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Netcode;

public class LobbyRoster : IDisposable
{
#region Fields
    public event Action<IReadOnlyList<string>> PlayersChanged;
#endregion

#region Private Members
    private readonly NetworkManager networkManager;
    private readonly List<string> connectedPlayers = new List<string>();
    private bool isTracking;
    private const string RosterUpdateMessageName = "LobbyRoster.Update";
#endregion

#region Getters
    public IReadOnlyList<string> PlayerIds => connectedPlayers;
#endregion

#region Unity Message Methods
#endregion

#region Public Methods
    public LobbyRoster(NetworkManager networkManager)
    {
        if (networkManager == null)
            throw new ArgumentNullException(nameof(networkManager));

        this.networkManager = networkManager;
    }

    public void StartTracking()
    {
        if (isTracking)
            return;

        isTracking = true;
        connectedPlayers.Clear();
        RegisterMessageHandler();
        RegisterExistingClients();
        SubscribeToCallbacks();
        RaisePlayersChanged();
        BroadcastRoster();
    }

    public void StopTracking()
    {
        if (!isTracking)
            return;

        isTracking = false;
        UnsubscribeFromCallbacks();
        UnregisterMessageHandler();
        connectedPlayers.Clear();
        RaisePlayersChanged();
    }

    public void Dispose()
    {
        StopTracking();
    }
#endregion

#region Private Methods
    private void RegisterExistingClients()
    {
        IReadOnlyList<ulong> clientIds = networkManager.ConnectedClientsIds;
        for (int i = 0; i < clientIds.Count; i++)
            AddPlayer(clientIds[i], false);
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!isTracking)
            return;

        AddPlayer(clientId, true);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!isTracking)
            return;

        RemovePlayer(clientId);
    }

    private void AddPlayer(ulong clientId, bool broadcast)
    {
        string playerId = ConvertClientIdToPlayerId(clientId);
        if (connectedPlayers.Contains(playerId))
            return;

        connectedPlayers.Add(playerId);
        RaisePlayersChanged();

        if (broadcast)
            BroadcastRoster();
    }

    private void RemovePlayer(ulong clientId)
    {
        string playerId = ConvertClientIdToPlayerId(clientId);
        bool removed = connectedPlayers.Remove(playerId);
        if (removed)
        {
            RaisePlayersChanged();
            BroadcastRoster();
        }
    }

    private string ConvertClientIdToPlayerId(ulong clientId)
    {
        return clientId.ToString();
    }

    private void RegisterMessageHandler()
    {
        if (networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(RosterUpdateMessageName, HandleRosterMessage);
    }

    private void UnregisterMessageHandler()
    {
        if (networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(RosterUpdateMessageName);
    }

    private void BroadcastRoster()
    {
        if (!networkManager.IsServer)
            return;

        if (networkManager.CustomMessagingManager == null)
            return;

        int capacity = CalculateWriterCapacity();
        using (FastBufferWriter writer = new FastBufferWriter(capacity, Allocator.Temp))
        {
            writer.WriteValueSafe((ushort)connectedPlayers.Count);
            for (int i = 0; i < connectedPlayers.Count; i++)
                writer.WriteValueSafe(connectedPlayers[i]);

            networkManager.CustomMessagingManager.SendNamedMessageToAll(RosterUpdateMessageName, writer, NetworkDelivery.ReliableSequenced);
        }
    }

    private void HandleRosterMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (!isTracking)
            return;

        ushort playerCount;
        if (!reader.TryBeginRead(sizeof(ushort)))
            return;

        reader.ReadValueSafe(out playerCount);

        connectedPlayers.Clear();
        for (ushort i = 0; i < playerCount; i++)
        {
            string playerId;
            if (!reader.TryBeginRead(sizeof(int)))
                break;

            reader.ReadValueSafe(out playerId);
            if (!string.IsNullOrWhiteSpace(playerId))
                connectedPlayers.Add(playerId);
        }

        RaisePlayersChanged();
    }

    private int CalculateWriterCapacity()
    {
        int capacity = sizeof(ushort);
        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            string playerId = connectedPlayers[i];
            int byteCount = Encoding.UTF8.GetByteCount(playerId);
            capacity += sizeof(int) + byteCount;
        }

        return Math.Max(capacity, 64);
    }

    private void SubscribeToCallbacks()
    {
        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void UnsubscribeFromCallbacks()
    {
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    private void RaisePlayersChanged()
    {
        if (PlayersChanged != null)
            PlayersChanged.Invoke(connectedPlayers);
    }
#endregion
}
