using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Deform : MonoBehaviour
{
    [SerializeField]
    private new Renderer renderer;

    private Vector4 pivot;
    private Tweener factor;

    public async UniTask StartAnimation()
    {
        await UniTask.WaitForSeconds(2);
        factor.Kill();
        factor = DOVirtual.Float(-1.5f, 1, 3, val =>
        {
            renderer.material.SetFloat("_DeformFactor", val);

        }).SetLoops(-1, LoopType.Yoyo);

        pivot.y = 10;
        renderer.material.SetVector("_CenterPivot", pivot);
    }
}
