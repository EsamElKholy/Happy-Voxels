using Cysharp.Threading.Tasks;
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

    private Dictionary<PlayerRef, FusionPlayer> spawnedPlayer = new();

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
                var obj = runner.Spawn(playerPrefab, onBeforeSpawned: (runner, obj) =>
                {
                    var fusionPlayer = obj.GetComponent<FusionPlayer>();
                    spawnedPlayer.Add(player, fusionPlayer);
                });
            }
            else
            {
                WaitForSpawnToCache(player).Forget();
            }
        }

        Debug.LogError($"Cached players {spawnedPlayer.Count}");
    }

    private async UniTask WaitForSpawnToCache(PlayerRef player) 
    {
        bool found = false;
        while (!found) 
        {
            var players = FindObjectsByType<FusionPlayer>(sortMode: FindObjectsSortMode.None);

            foreach (var p in players)
            {
                if (p.Object.StateAuthority == player)
                {
                    spawnedPlayer.Add(player, p);
                    found = true;
                    break;
                }
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        Debug.LogError($"Cached players {spawnedPlayer.Count}");
    }

    private void PlayerLeft(PlayerRef player) 
    {
        if (player == runner.LocalPlayer) 
        {
            if (spawnedPlayer.ContainsKey(player))
            {
                var fusionPlayer = spawnedPlayer[player];
                spawnedPlayer.Remove(player);

                if (fusionPlayer)
                {
                    if (fusionPlayer.Object)
                    {
                        runner.Despawn(fusionPlayer.Object);
                    }
                }
            }
        }
        else
        {
            if (spawnedPlayer.ContainsKey(player))
            {
                spawnedPlayer.Remove(player);
            }
        }

        Debug.LogError($"Cached players {spawnedPlayer.Count}");
    }
}
