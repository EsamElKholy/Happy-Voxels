using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshVoxelizerController : MonoBehaviour
{
    [ReadOnly]
    [SerializeField]
    private int totalFilledVoxels = 0;

    [ReadOnly]
    [SerializeField]
    private int totalVoxels = 0;

    private MeshVoxelizer[] voxelizersInChildren;
    private int[] filledVoxelCounts;
    private int[] totalVoxelCounts;
    private void Awake()
    {
        voxelizersInChildren = GetComponentsInChildren<MeshVoxelizer>();
        filledVoxelCounts = new int[voxelizersInChildren.Length];
        totalVoxelCounts = new int[voxelizersInChildren.Length];
    }

    [Button]
    private void Voxelize() 
    {
        for (int i = 0; i < voxelizersInChildren.Length; i++)
        {
            var voxelizer = voxelizersInChildren[i];
            VoxelizeAsync(voxelizer, i).Forget();
        }

        CalculateVoxelCount().Forget();
    }

    private async UniTask CalculateVoxelCount() 
    {
        if (filledVoxelCounts != null)
        {
            for (int i = 0; i < filledVoxelCounts.Length; i++)
            {
                await UniTask.WaitUntil(() => { return filledVoxelCounts[i] > 0; });
                totalFilledVoxels += filledVoxelCounts[i];
                totalVoxels += totalVoxelCounts[i];
            }
        }
    }

    [Button]
    private void ResetToOriginal()
    {
        foreach (var voxelizer in voxelizersInChildren)
        {
            voxelizer.ResetToOriginal();
        }

        totalFilledVoxels = 0;
        totalVoxels = 0;
    }

    private async UniTask VoxelizeAsync(MeshVoxelizer voxelizer, int index) 
    {
        await UniTask.WaitForSeconds(Random.Range(0.5f, 2.5f));

        voxelizer.Voxelize();

        await UniTask.WaitUntil(() => { return voxelizer.GetFilledNodeCount() != 0; });
        filledVoxelCounts[index] = voxelizer.GetFilledNodeCount();
        totalVoxelCounts[index] = voxelizer.GetTotalVoxelCount();
    }
}
