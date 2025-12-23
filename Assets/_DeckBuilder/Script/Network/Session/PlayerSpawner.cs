using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    public override void OnNetworkSpawn()
    {
        if (!IsHost)
            return;


        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            SpawnPlayerFor(clientId);

        // Subscribe to future connections. Not handled currently
        NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerFor;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayerFor;
    }

    private void SpawnPlayerFor(ulong clientId)
    {
        var client = NetworkManager.Singleton.ConnectedClients[clientId];

        Debug.Log("Spawning player for client " + clientId);

        if (client.PlayerObject != null) return;


        var go = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        var no = go.GetComponent<NetworkObject>();
        no.SpawnAsPlayerObject(clientId);
    }
}
