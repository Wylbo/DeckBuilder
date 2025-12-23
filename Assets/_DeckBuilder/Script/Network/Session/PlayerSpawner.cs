using System;
using UnityEngine;
using Unity.Netcode;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    public GameObject PlayerPrefab => playerPrefab;

    private void Start()
    {
        if (!IsServer)
            return;

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
            networkObject = gameObject.AddComponent<NetworkObject>();

        if (!networkObject.IsSpawned)
        {
            networkObject.Spawn();
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
            return;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerFor(clientId);
        }
    }
    void SpawnPlayerFor(ulong clientId)
    {
        if (!NetworkManager.Singleton)
        {
            return;
        }

        var player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkObject networkObject = player.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId, true);
    }

}