using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalSpawnLocation : SimulationBehaviour
{
    [SerializeField]
    private NetworkPrefabRef spawnLocationPrefab;

    private PlayerSpawnLocation spawnLocation;
    public Action<PlayerSpawnLocation> OnSpawnLocationSet;

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

    private void ConnectedToServer(NetworkRunner runner)
    {
        var obj = runner.Spawn(spawnLocationPrefab, position: transform.position, rotation: transform.rotation, onBeforeSpawned: (runner, obj) => 
        {
            obj.transform.SetParent(transform);
            spawnLocation = obj.GetComponent<PlayerSpawnLocation>();
            OnSpawnLocationSet?.Invoke(spawnLocation);
        });
    }

    private void DisconnectedFromServer(NetworkRunner runner)
    {
        if (spawnLocation)
        {
            runner.Despawn(spawnLocation.Object);
        }
    }
}
