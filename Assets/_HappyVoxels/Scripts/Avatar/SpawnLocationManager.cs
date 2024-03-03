using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnLocationManager : MonoBehaviour
{
    [SerializeField]
    private List<Transform> spawnLocations = new List<Transform>();

    public Transform GetSpawnLocation(int index) 
    {
        if (spawnLocations.Count <= index || index < 0) 
        {
            return null;
        }

        return spawnLocations[index];
    }
}
