using Cysharp.Threading.Tasks;
using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FusionGunController : NetworkBehaviour
{
    private bool wasFiring = false;

    private GameObject gun;
    private ParticleSystem fireParticles;
    private bool isInitialized = false;
    private bool underCooldown = false;
    private float cooldownDuration = 0.15f;
    private float cooldownTimer = 0;
    private float lastTimeFired = 0;

    public override void Spawned()
    {
        base.Spawned();
    }

    public void Initialize(GameObject gun) 
    {
        this.gun = gun;
        fireParticles = this.gun.GetComponentInChildren<ParticleSystem>();

        if (HasStateAuthority)
        {
            isInitialized = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!isInitialized)
        {
            return;
        }

        if (GetInput(out NetworkInputData networkInputData))
        {
            if (networkInputData.isFiring)
            {
                if (!underCooldown)
                {
                    cooldownTimer = 0;
                    underCooldown = true;
                    RPC_Shoot(networkInputData.mouseAim);
                }
            }            
        }

        if (underCooldown)
        {
            cooldownTimer += Runner.DeltaTime;

            if (cooldownTimer >= cooldownDuration)
            {
                cooldownTimer = 0;
                underCooldown = false;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Shoot(Vector3 aim) 
    {
        if (fireParticles.isPlaying)
        {
            fireParticles.Stop();
        }

        fireParticles.Play();
    }
}
