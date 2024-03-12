using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "Voxel Mesh Scene Data", menuName = "Scriptable Objects/Voxel Mesh Scene Data")]
public class VoxelMeshSceneDataSO : ScriptableObject
{
    public List<string> voxelMeshInScene = new List<string>();

    [Button]
    private void GetVoxelMeshesInScene() 
    {
        voxelMeshInScene.Clear();

        var roots = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach ( var root in roots ) 
        {
            var voxelController = root.GetComponent<MeshVoxelizerController>();
            if ( voxelController != null ) 
            {
                voxelMeshInScene.Add(voxelController.VoxelizerName);            
            }
        }
    }
}
