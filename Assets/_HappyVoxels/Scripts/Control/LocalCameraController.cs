using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCameraController : MonoBehaviour
{
    [SerializeField]
    private Vector2 cameraSensitivity;

    private Camera localCamera;
    private Transform followTarget;
    private Vector2 cameraRotation;

    private void Awake()
    {
        localCamera = GetComponent<Camera>();
    }

    public void SetFollowTarget(Transform target) 
    {
        followTarget = target;

        if (localCamera)
        {
            localCamera.transform.parent = null;
        }
    }

    private void LateUpdate()
    {
        if (!localCamera || !localCamera.gameObject.activeSelf) 
        {
            return;
        }

        if (!followTarget)
        {
            return;
        }        

        transform.position = followTarget.position;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y") * -1;
        float cameraRotationAroundX = mouseY * Time.deltaTime * cameraSensitivity.x;
        cameraRotation.x += cameraRotationAroundX;
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -80, 80);

        float cameraRotationAroundY = mouseX * Time.deltaTime * cameraSensitivity.y;
        cameraRotation.y += cameraRotationAroundY;

        localCamera.transform.localEulerAngles = new Vector3(cameraRotation.x, cameraRotation.y, 0);
    }
}
