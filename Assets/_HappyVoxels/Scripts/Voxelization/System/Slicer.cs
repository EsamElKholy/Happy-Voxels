using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slicer : MonoBehaviour
{

    [SerializeField]
    private bool topToBottom = true;
    [SerializeField]
    private float sliceDuration = 2;

    private GameObject plane;
    private GameObject meshToSlice;

    private Plane slicingPlane;
    private Vector4 equation;
    private Vector3 topPosition;
    private Vector3 bottomPosition;

    public Action OnFinishedSlicing;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 up = transform.up;

        slicingPlane = new Plane(up, transform.position);
        equation = new Vector4(slicingPlane.normal.x, slicingPlane.normal.y, slicingPlane.normal.z, slicingPlane.distance);
        meshToSlice = gameObject;
        var renderer = meshToSlice.GetComponent<Renderer>();

        topPosition = ModelUtils.GetTopCenter(renderer.gameObject);
        bottomPosition = ModelUtils.GetBottomCenter(renderer.gameObject);

        if (!plane)
        {
            plane = new GameObject("SlicingPlane_" + gameObject.name);
        }
    }

    [Button]
    public void StartSlicing() 
    {
        StartSlicingAsync().Forget();
    }

    private async UniTask StartSlicingAsync() 
    {
        await UniTask.WaitForSeconds(UnityEngine.Random.Range(1.5f, 3));

        ResetPlanePosition(!topToBottom);

        float counter = 0;
        while (counter < sliceDuration) 
        {
            counter += Time.deltaTime;
            Vector3 newPos = Vector3.zero;
            if (topToBottom)
            {
                newPos = Vector3.Lerp(topPosition, bottomPosition, counter / sliceDuration);
            }
            else
            {
                newPos = Vector3.Lerp(bottomPosition, topPosition, counter / sliceDuration);
            }

            plane.transform.position = newPos;
            UpdateEquation();
            UpdateMaterials();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        OnFinishedSlicing?.Invoke();       
    }

    public Vector4 GetEquation()
    {
        return equation;
    }

    public void UpdateEquation()
    {
        if (transform)
        {
            Vector3 up = plane.transform.up;

            slicingPlane.SetNormalAndPosition(up, plane.transform.position);

            equation.x = slicingPlane.normal.x;
            equation.y = slicingPlane.normal.y;
            equation.z = slicingPlane.normal.z;
            equation.w = slicingPlane.distance;
        }
    }

    public void ResetPlanePosition(bool toTop)
    {
        if (meshToSlice)
        {
            var renderer = meshToSlice.GetComponent<Renderer>();
            topPosition = ModelUtils.GetTopCenter(renderer.gameObject);
            bottomPosition = ModelUtils.GetBottomCenter(renderer.gameObject);

            if (toTop)
            {
                topPosition.y += Vector3.Distance(topPosition, bottomPosition) * 0.05f;
                plane.transform.position = topPosition;
            }
            else
            {
                bottomPosition.y -= Vector3.Distance(topPosition, bottomPosition) * 0.02f;
                plane.transform.position = bottomPosition;
            }

            UpdateEquation();
            UpdateMaterials();
        }
    }

    private void UpdateMaterials()
    {
        var renderer = meshToSlice.GetComponent<Renderer>();
        for (int i = 0; i < renderer.materials.Length; i++)
        {
            renderer.materials[i].SetVector("_SlicingPlane", GetEquation());
        }
    }
}
