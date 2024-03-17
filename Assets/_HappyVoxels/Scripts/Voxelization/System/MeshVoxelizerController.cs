using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshVoxelizerController : MonoBehaviour
{
    [SerializeField]
    private string voxelizerName;

    [ReadOnly]
    [SerializeField]
    private int totalFilledVoxels = 0;

    [ReadOnly]
    [SerializeField]
    private int totalVoxels = 0;

    [SerializeField]
    private bool useGlobalDepth;

    [ShowIf("useGlobalDepth")]
    [Range(1, 10)]
    [SerializeField]
    private int treeDepth = 1;

    private MeshVoxelizer[] voxelizersInChildren;
    private int[] filledVoxelCounts;
    private int[] totalVoxelCounts;

    public string VoxelizerName { get { return voxelizerName; } }
    private bool isInitialized = false;

    private void Awake()
    {
        voxelizersInChildren = GetComponentsInChildren<MeshVoxelizer>();
        filledVoxelCounts = new int[voxelizersInChildren.Length];
        totalVoxelCounts = new int[voxelizersInChildren.Length];
        isInitialized = true;
    }

    [Button]
    public async UniTask Voxelize() 
    {
        await UniTask.WaitUntil(() => { return isInitialized; });
        totalVoxels = 0;
        totalFilledVoxels = 0;

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
    public void ResetToOriginal()
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
        await UniTask.WaitForSeconds(Random.Range(0.1f, 0.75f));
        if (useGlobalDepth)
        {
            voxelizer.Voxelize(treeDepth);
        }
        else
        {
            voxelizer.Voxelize();
        }

        await UniTask.WaitUntil(() => { return voxelizer.GetFilledNodeCount() != 0; });
        filledVoxelCounts[index] = voxelizer.GetFilledNodeCount();
        totalVoxelCounts[index] = voxelizer.GetTotalVoxelCount();
    }
}
