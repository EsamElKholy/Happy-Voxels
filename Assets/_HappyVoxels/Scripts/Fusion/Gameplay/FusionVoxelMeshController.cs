using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FusionVoxelMeshController : NetworkBehaviour
{
    [Networked, Capacity(10), OnChangedRender(nameof(OnVoxelStateChanged))]
    private NetworkDictionary<string, bool> VoxelStates { get; } = MakeInitializer(new Dictionary<string, bool>());

    private VoxelMeshScene voxelMeshScene;

    private List<Vector2> addedVoxels = new List<Vector2>();
    private List<Vector2> removedVoxels = new List<Vector2>();

    public override void Spawned()
    {
        base.Spawned();

        voxelMeshScene = FindAnyObjectByType<VoxelMeshScene>();

        if (HasStateAuthority)
        {
            if (VoxelStates.Count == 0)
            {
                var names = voxelMeshScene.GetMeshVoxelizerControllerNames();
                foreach (var name in names) 
                {
                    VoxelStates.Add(name, voxelMeshScene.StartVoxelized);                
                }
            }
        }
    }

    private void OnVoxelStateChanged() 
    {
        if (voxelMeshScene)
        {
            foreach (var state in VoxelStates) 
            {
                var controller = voxelMeshScene.GetMeshVoxelizerController(state.Key);            
                if (state.Value)
                {
                    controller.Voxelize(); 
                }
                else
                {
                    controller.ResetToOriginal();
                }
            }
        }
    }
}
