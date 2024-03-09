using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnLocationManager : MonoBehaviour
{
    private List<SpawnLocation> spawnLocations = new List<SpawnLocation>();

    private void Awake()
    {
        ReloadSpawnLocations();

        SceneManager.activeSceneChanged += ActiveSceneChanged;
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= ActiveSceneChanged;
    }

    private void ActiveSceneChanged(Scene arg0, Scene arg1)
    {
        ReloadSpawnLocations();
    }

    private void ReloadSpawnLocations() 
    {
        spawnLocations.Clear();
        var locations = FindObjectsByType<SpawnLocation>(sortMode: FindObjectsSortMode.None);

        spawnLocations.AddRange(locations);
    }

    public Transform GetSpawnLocation(int index) 
    {
        if (spawnLocations.Count <= index || index < 0) 
        {
            index = index < 0 ? 0 : index % spawnLocations.Count;
        }

        return spawnLocations[index].transform;
    }
}
