using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FusionPlayerManager : SimulationBehaviour
{  
    private NetworkRunner runner;

    [SerializeField]
    private NetworkPrefabRef playerPrefab;

    [SerializeField]
    private List<Transform> playerSpawnLocations = new();

    private Dictionary<PlayerRef, NetworkObject> spawnedPlayer = new();

    private void Awake()
    {
        RunnerCallback.OnConnectedToServerCallback += ConnectedToServer;
        RunnerCallback.OnDisconnectedFromServerCallback += DisconnectedFromServer;
    }

    private void OnDestroy()
    {
        RunnerCallback.OnConnectedToServerCallback -= ConnectedToServer;
        RunnerCallback.OnDisconnectedFromServerCallback -= DisconnectedFromServer;
    }

    private void Initialize()
    {
        RunnerCallback.OnPlayerJoinedSession += PlayerJoined;       
        RunnerCallback.OnPlayerLeftSession += PlayerLeft;
    }

    private void ConnectedToServer(NetworkRunner runner)
    {
        this.runner = runner;
        Initialize();
    }

    private void DisconnectedFromServer(NetworkRunner runner) 
    {
        Clear();
    }

    public void Clear()
    {
        RunnerCallback.OnPlayerJoinedSession -= PlayerJoined;
        RunnerCallback.OnPlayerLeftSession -= PlayerLeft;       
    }

    private void PlayerJoined(PlayerRef player)
    {
        if (!spawnedPlayer.ContainsKey(player)) 
        {
            if (player == runner.LocalPlayer)
            {                
                var spawnLocation = playerSpawnLocations[player.AsIndex];

                runner.Spawn(playerPrefab, position: spawnLocation.position, spawnLocation.rotation, onBeforeSpawned: (runner, obj) =>
                {
                    spawnedPlayer.Add(player, obj);
                });
            }
        }
    }

    private void PlayerLeft(PlayerRef player) 
    {
        if (spawnedPlayer.ContainsKey(player))
        {
            var playerObj = spawnedPlayer[player];

            runner.Despawn(playerObj);            
        }
    }
}
