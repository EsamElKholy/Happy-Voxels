using Cysharp.Threading.Tasks;
using Fusion;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class FusionPlayer : NetworkBehaviour
{
    #region SerializedFields
    [SerializeField]
    private List<PlayerAvatar> avatarPrefabs = new();

    [SerializeField]
    private FusionPlayerController fusionPlayerController;

    [SerializeField]
    private Camera cameraPrefab;
    [SerializeField]
    private GameObject gunPrefab;
    #endregion

    #region NetworkedProperties
    [HideInInspector]
    [Networked, OnChangedRender(nameof(OnCurrentAvatarTypeChanged))]
    public AvatarType CurrentAvatarType { get; private set; } = AvatarType.NONE;
    #endregion
   
    
    public int PlayerIndex { get; private set; } = -1;   
    private PlayerAvatar currentAvatar;
    private SpawnLocationManager spawnLocationManager;
    private AvatarType defaultAvatarType = AvatarType.Default;
    private GameObject gun;
    
    public PlayerAvatar CurrentAvatar { get { return currentAvatar; } }
    public LocalCameraController LocalCameraController { get; private set; }

    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
        {
            PlayerIndex = Object.StateAuthority.AsIndex - 1;

            Debug.LogError($"Player id {PlayerIndex}, State {HasStateAuthority}");

            spawnLocationManager = FindFirstObjectByType<SpawnLocationManager>();

            if (spawnLocationManager)
            {
                var spawnLocation = spawnLocationManager.GetSpawnLocation(PlayerIndex);

                if (spawnLocation != null) 
                {
                    transform.position = spawnLocation.position;
                    transform.rotation = spawnLocation.rotation;
                }
            }

            fusionPlayerController.Initialize();
            CurrentAvatarType = defaultAvatarType;
        }
        else
        {
            ChangeAvatar(CurrentAvatarType);
        }
    }

    public void ChangeAvatar(AvatarType avatarType) 
    {
        if (avatarType == AvatarType.NONE)
        {
            return;
        }

        int index = avatarPrefabs.FindIndex(x => x.GetComponent<PlayerAvatar>().AvatarType == avatarType);
        Quaternion currentCameraRotation = Quaternion.identity;
        if (index != -1)
        {
            if (currentAvatar)
            {
                Destroy(currentAvatar.gameObject);
            }

            var avatar = Instantiate(avatarPrefabs[index], transform);

            var playerAvatar = avatar.GetComponent<PlayerAvatar>();
            currentAvatar = playerAvatar;
        }

        if (HasStateAuthority && !LocalCameraController)
        {
            InitializeCamera();
        }

        if (!gun)
        {
            SpawnGun();
        }
    }

    private void InitializeCamera() 
    {
        Instantiate(cameraPrefab, transform);

        LocalCameraController = GetComponentInChildren<LocalCameraController>(true);
        currentAvatar.GunSpawnLocation.SetParent(LocalCameraController.transform);

        LocalCameraController.gameObject.SetActive(true);
        LocalCameraController.SetFollowTarget(transform);
    }

    private void SpawnGun() 
    {
        gun = Instantiate(gunPrefab, currentAvatar.GunSpawnLocation);
    }

    private void OnCurrentAvatarTypeChanged()
    {
        ChangeAvatar(CurrentAvatarType);
    }

    #region RPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ChangeAvatar(AvatarType avatarType) 
    {
        defaultAvatarType = avatarType;
        CurrentAvatarType = defaultAvatarType;
    }
    #endregion      

#if UNITY_EDITOR
    public void Debug_ChangeDefaultAvatarType(AvatarType avatarType)
    {
        defaultAvatarType = avatarType;

        if (Object && Object.IsValid && CurrentAvatarType != defaultAvatarType)
        {
            RPC_ChangeAvatar(defaultAvatarType);
        }
    }
#endif
}
