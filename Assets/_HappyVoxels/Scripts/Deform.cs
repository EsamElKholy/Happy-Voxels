using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deform : MonoBehaviour
{
    [SerializeField]
    private float speed = 100;
    [SerializeField]
    private float amount = 0.1f;
   
    [SerializeField]
    private new Renderer renderer;

    private Vector4 pivot;
    private float deformFactor = 0;
    private Tweener factor;
    private Tweener pivotY;

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

    private void UpdateMaterial()
    {
        if (renderer.material)
        {
            renderer.material.SetVector("_CenterPivot", pivot);
            renderer.material.SetFloat("_DeformFactor", deformFactor);
        }
    }

    private void ModifyX(float amount)
    {
        pivot.x += amount;
    }

    private void ModifyY(float amount)
    {
        pivot.y += amount;
    }

    private void ModifyZ(float amount)
    {
        pivot.z += amount;
    }

    private void ModifyFactor(float amount)
    {
        deformFactor += amount;

        if (deformFactor < 0)
        {
            deformFactor = 0;
        }
    }
}
