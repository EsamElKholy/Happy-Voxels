using Cysharp.Threading.Tasks;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FusionSpawnLocationManager : SimulationBehaviour
{
    private LocalSpawnLocation[] localSpawnLocations = null;
    private List<PlayerSpawnLocation> playerSpawnLocations = new List<PlayerSpawnLocation>();

    private void Awake()
    {
        RegisterToSpawnLocationCallbacks();
        FusionPlayerManager.OnPlayerJoined += PlayerJoined;
        SceneManager.activeSceneChanged += ActiveSceneChanged;
    }    

    private void OnDestroy()
    {
        FusionPlayerManager.OnPlayerJoined -= PlayerJoined;
    }

    private void PlayerJoined(FusionPlayer player)
    {
        SetSpawnLocation(player).Forget();
    }

    private void ActiveSceneChanged(Scene scene1, Scene scene2)
    {
        RegisterToSpawnLocationCallbacks();
    }

    private void RegisterToSpawnLocationCallbacks()
    {
        playerSpawnLocations.Clear();

        if (localSpawnLocations != null && localSpawnLocations.Length > 0)
        {
            foreach (var item in localSpawnLocations)
            {
                item.OnSpawnLocationSet -= PlayerSpawnLocationSet;
            }
        }       

        localSpawnLocations = FindObjectsByType<LocalSpawnLocation>(findObjectsInactive: FindObjectsInactive.Exclude, sortMode: FindObjectsSortMode.None);

        foreach (var item in localSpawnLocations)
        {
            item.OnSpawnLocationSet += PlayerSpawnLocationSet;
        }
    }

    private void PlayerSpawnLocationSet(PlayerSpawnLocation location)
    {
        playerSpawnLocations.Add(location);
    }

    private async UniTask SetSpawnLocation(FusionPlayer player) 
    {

        await UniTask.WaitUntil(() => { return playerSpawnLocations.Count == localSpawnLocations.Length && playerSpawnLocations.FindIndex(x => x.Object.IsValid == false) == -1; });

        var spawnLocationIndex = playerSpawnLocations.FindIndex(x => x.IsUsed == false);
        if (spawnLocationIndex != -1)
        {
            playerSpawnLocations[spawnLocationIndex].IsUsed = true;
            player.MoveToSpawnLocation(playerSpawnLocations[spawnLocationIndex].transform.position);
        }
    }
}
