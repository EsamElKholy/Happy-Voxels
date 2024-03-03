using Cysharp.Threading.Tasks;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FusionPlayerController : NetworkBehaviour
{
    [SerializeField]
    private CharacterController characterController;
    [SerializeField]
    private float playerSpeed = 2;   
    [SerializeField]
    private float respawnHeight = -10;

    private bool isInitialized = false;
    private FusionPlayer fusionPlayer;
    private SpawnLocationManager spawnLocationManager;

    public override void Spawned()
    {
        base.Spawned();

        if (!HasStateAuthority)
        {
            enabled = false;
        }
    }

    public void Initialize() 
    {       
        characterController.Move(transform.position);
        fusionPlayer = GetComponent<FusionPlayer>();
        spawnLocationManager = FindFirstObjectByType<SpawnLocationManager>();
        spawnLocationManager = FindFirstObjectByType<SpawnLocationManager>();
        isInitialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!isInitialized)
        {
            return;
        }       

        if (fusionPlayer && fusionPlayer.CurrentAvatar && fusionPlayer.CurrentAvatar.LocalCameraController)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, fusionPlayer.CurrentAvatar.LocalCameraController.transform.localEulerAngles.y, transform.localEulerAngles.z);
        }

        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Runner.DeltaTime * playerSpeed;

        characterController.Move(transform.TransformDirection(move));
        CheckRespawn();
    }

    private void CheckRespawn() 
    {
        if (transform.position.y <= respawnHeight)
        {
            var spawnLocation = spawnLocationManager.GetSpawnLocation(fusionPlayer.PlayerIndex);

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
