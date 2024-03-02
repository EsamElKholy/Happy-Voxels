using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FusionPlayerManager : SimulationBehaviour
{  
    private NetworkRunner runner;

    [SerializeField]
    private NetworkPrefabRef playerPrefab;

    [SerializeField]
    private List<Transform> playerSpawnLocations = new();

    private Dictionary<PlayerRef, FusionPlayer> spawnedPlayer = new();

    public static Action<FusionPlayer> OnPlayerJoined;
    public static Action<FusionPlayer> OnPlayerLeft;

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

                runner.Spawn(playerPrefab, onBeforeSpawned: (runner, obj) =>
                {
                    var fusionPlayer = obj.GetComponent<FusionPlayer>();

                    spawnedPlayer.Add(player, fusionPlayer);

                    OnPlayerJoined?.Invoke(fusionPlayer);
                });
            }
        }
    }

    private void PlayerLeft(PlayerRef player) 
    {
        if (spawnedPlayer.ContainsKey(player))
        {
            var fusionPlayer = spawnedPlayer[player];

            OnPlayerLeft?.Invoke(fusionPlayer);
            runner.Despawn(fusionPlayer.Object);            
        }
    }
}
