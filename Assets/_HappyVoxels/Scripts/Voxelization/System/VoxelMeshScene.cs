using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VoxelMeshScene : MonoBehaviour
{
    [SerializeField]
    private List<MeshVoxelizerController> voxelMeshControllerInScene = new();

    [SerializeField]
    private bool startVoxelized = false;

    public bool StartVoxelized { get { return startVoxelized; } }

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

    public List<string> GetMeshVoxelizerControllerNames() 
    {
        return voxelMeshControllerInScene.Select(x => x.VoxelizerName).ToList();
    }
}
