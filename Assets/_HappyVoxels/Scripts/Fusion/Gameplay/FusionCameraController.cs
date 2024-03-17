using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FusionCameraController : NetworkBehaviour
{
    [SerializeField]
    private Vector2 cameraSensitivity;

    private Camera localCamera;
    private Transform followTarget;
    private Vector2 cameraRotation;
    private bool isInitialized = false;

    public void Initialize(Camera camera, Transform target)
    {
        followTarget = target;
        localCamera = camera;

        if (localCamera)
        {
            localCamera.transform.parent = null;
        }
        isInitialized = true;
    }

    public void Pause() 
    {
        isInitialized = false;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!localCamera || !localCamera.gameObject.activeSelf)
        {
            return;
        }

        if (!followTarget)
        {
            return;
        }

        if (!isInitialized)
        {
            return;
        }

        localCamera.transform.position = followTarget.position;

        if (GetInput(out NetworkInputData networkInputData))
        {
            Vector2 aim = networkInputData.mouseAim;

            float cameraRotationAroundX = aim.y * Runner.DeltaTime * cameraSensitivity.x;
            cameraRotation.x += cameraRotationAroundX;
            cameraRotation.x = Mathf.Clamp(cameraRotation.x, -80, 80);

            float cameraRotationAroundY = aim.x * Runner.DeltaTime * cameraSensitivity.y;
            cameraRotation.y += cameraRotationAroundY;
            cameraRotation.y = cameraRotation.y % 359;
            localCamera.transform.localEulerAngles = new Vector3(cameraRotation.x, cameraRotation.y, 0);
        }
    }
}
