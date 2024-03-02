using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FusionPlayerController : NetworkBehaviour
{
    [SerializeField]
    private CharacterController characterController;
    [SerializeField]
    private float playerSpeed = 2;

    private bool isInitialized = false;

    public override void Spawned()
    {
        base.Spawned();

        if (!HasStateAuthority)
        {
            enabled = false;
        }
    }

    public void Initialize(NetworkBehaviour parent) 
    {
        transform.SetParent(parent.transform);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        isInitialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!isInitialized)
        {
            return;
        }

        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Runner.DeltaTime * playerSpeed;

        characterController.Move(move);

        if (move != Vector3.zero)
        {
            transform.forward = move;
        }
    }
}
