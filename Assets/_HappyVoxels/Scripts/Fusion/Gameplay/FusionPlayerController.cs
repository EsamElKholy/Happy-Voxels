using Cysharp.Threading.Tasks;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class FusionPlayerController : NetworkBehaviour
{
    [SerializeField]
    private CharacterController characterController;
    [SerializeField]
    private float playerSpeed = 2;   
    [SerializeField]
    private float respawnHeight = -10;
    [SerializeField]
    private float gravityValue = 9.81f;

    private bool isInitialized = false;
    private FusionPlayer fusionPlayer;
    private float verticalVelocity;
    private FusionPlayer player;

    public override void Spawned()
    {
        base.Spawned();

        if (!HasStateAuthority)
        {
            enabled = false;
        }
    }

    public void Initialize(FusionPlayer player) 
    {       
        characterController.Move(transform.position);
        fusionPlayer = GetComponent<FusionPlayer>();
        this.player = player;
        isInitialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!isInitialized)
        {
            return;
        }

        if (fusionPlayer && fusionPlayer.CurrentAvatar && fusionPlayer.LocalCamera)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, fusionPlayer.LocalCamera.transform.localEulerAngles.y, transform.localEulerAngles.z);
        }

        if (GetInput(out NetworkInputData networkInputData))
        {
            bool groundedPlayer = characterController.isGrounded;

            if (groundedPlayer && verticalVelocity < 0)
            {
                verticalVelocity = 0f;
            }

            verticalVelocity -= gravityValue * Runner.DeltaTime;

            Vector3 move = new Vector3(networkInputData.movementInput.x, verticalVelocity, networkInputData.movementInput.y) * Runner.DeltaTime * playerSpeed;

            characterController.Move(transform.TransformDirection(move));

            if (HasStateAuthority)
            {
                if (networkInputData.changeToRed)
                {
                    player.Debug_ChangeDefaultAvatarType(AvatarType.Red);
                }

                if (networkInputData.changeToGreen)
                {
                    player.Debug_ChangeDefaultAvatarType(AvatarType.Green);
                }

                if (networkInputData.changeToBlue)
                {
                    player.Debug_ChangeDefaultAvatarType(AvatarType.Blue);
                }
            }            
        }

        CheckRespawn();
    }

    private void CheckRespawn() 
    {
        if (transform.position.y <= respawnHeight)
        {
            var spawnLocation = SingletonInterface.SingletonLocator.SpawnLocationManager.GetSpawnLocation(fusionPlayer.PlayerIndex);

            if (spawnLocation != null) 
            {
                transform.position = spawnLocation.position;
                transform.rotation = spawnLocation.rotation;
            }
            else
            {
                transform.position = Vector3.zero;
                transform.rotation = Quaternion.identity;
            }
        }
    }
}
