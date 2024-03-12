using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FusionVoxelMeshController : NetworkBehaviour
{
    [Networked, Capacity(10), OnChangedRender(nameof(OnVoxelStateChanged))]
    private NetworkDictionary<string, bool> voxelStates {  get; set; } = MakeInitializer(new Dictionary<string, bool>());

    private VoxelMeshScene voxelMeshScene;

    private List<Vector2> addedVoxels = new List<Vector2>();
    private List<Vector2> removedVoxels = new List<Vector2>();

    public override void Spawned()
    {
        base.Spawned();

        voxelMeshScene = FindAnyObjectByType<VoxelMeshScene>();
    }

    private void OnVoxelStateChanged() 
    {
        if (voxelMeshScene)
        {
            foreach (var state in voxelStates) 
            {
                var controller = voxelMeshScene.GetMeshVoxelizerController(state.Key);            
                if (state.Value)
                {
                    controller.ResetToOriginal();
                }
                else
                {
                    controller.Voxelize(); 
                }
            }
        }
    }
}
