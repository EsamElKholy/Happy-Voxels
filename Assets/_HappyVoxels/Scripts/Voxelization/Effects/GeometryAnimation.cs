using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class GeometryAnimation : MonoBehaviour
{
    private MeshRenderer voxelRenderer;

    void Start()
    {
        voxelRenderer = GetComponent<MeshRenderer>();
        voxelRenderer.material = new Material(voxelRenderer.material);

        StartAnimation(5).Forget();
    }

    public async UniTask StartAnimation(float delay) 
    {
        await UniTask.WaitForSeconds(delay);

        DOVirtual.Float(0.46f, 0.1f, 2, val =>
        {
            voxelRenderer.material.SetFloat("_VoxelSize", val);
        }).SetLoops(-1, LoopType.Yoyo);
    }
}
