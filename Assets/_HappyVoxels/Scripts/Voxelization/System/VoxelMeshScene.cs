using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VoxelMeshScene : MonoBehaviour
{
    [SerializeField]
    private List<MeshVoxelizerController> voxelMeshControllerInScene = new();

    private void Awake()
    {
        voxelMeshControllerInScene.Clear();

        var roots = SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (var root in roots)
        {
            var voxelController = root.GetComponent<MeshVoxelizerController>();
            if (voxelController != null)
            {
                voxelMeshControllerInScene.Add(voxelController);
            }
        }
    }

    public MeshVoxelizerController GetMeshVoxelizerController(string name) 
    {
        return voxelMeshControllerInScene.Find(x => x.VoxelizerName == name);
    }
}
