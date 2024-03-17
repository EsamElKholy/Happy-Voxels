using Cysharp.Threading.Tasks;
using Fusion;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FusionPlayer : NetworkBehaviour
{
    #region SerializedFields
    [SerializeField]
    private List<PlayerAvatar> avatarPrefabs = new();

    [SerializeField]
    private AvatarType defaultAvatarType = AvatarType.Default;

    [SerializeField]
    private CharacterInputHandler characterInputHandler;

    [SerializeField]
    private FusionPlayerController fusionPlayerController;
    [SerializeField]
    private FusionGunController fusionGunController;
    [SerializeField]
    private FusionCameraController fusionCameraController;
    [SerializeField]
    private FusionVoxelMeshController fusionVoxelMeshController;

    [SerializeField]
    private Camera cameraPrefab;
    [SerializeField]
    private GameObject gunPrefab;
    #endregion

    #region NetworkedProperties
    [HideInInspector]
    [Networked, OnChangedRender(nameof(OnCurrentAvatarTypeChanged))]
    public AvatarType CurrentAvatarType { get; set; } = AvatarType.NONE;
    #endregion
   
    
    public int PlayerIndex { get; private set; } = -1;   
    private PlayerAvatar currentAvatar;
    private GameObject gun;
    private Camera localCamera;

    public PlayerAvatar CurrentAvatar { get { return currentAvatar; } }
    public FusionCameraController FusionCameraController { get { return fusionCameraController; } }
    public Camera LocalCamera { get { return localCamera; } }

    public override void Spawned()
    {
        base.Spawned();

        if (HasStateAuthority)
        {
            PlayerIndex = Object.StateAuthority.AsIndex - 1;

            var spawnLocation = SingletonInterface.SingletonLocator.SpawnLocationManager.GetSpawnLocation(PlayerIndex);

            if (spawnLocation != null)
            {
                transform.position = spawnLocation.position;
                transform.rotation = spawnLocation.rotation;
            }

            characterInputHandler.Initialize();
            fusionPlayerController.Initialize(this);
            CurrentAvatarType = defaultAvatarType;            
        }
        else
        {
            ChangeAvatar(CurrentAvatarType).Forget();
        }
    }

    public async UniTask ChangeAvatar(AvatarType avatarType) 
    {
        await UniTask.WaitForSeconds(1);
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

        if (HasStateAuthority)
        {
            currentAvatar.Initialize();
            InitializeCamera();
        }

        SpawnGun();
    }

    private void InitializeCamera() 
    {
        fusionCameraController.Pause();
        if (localCamera)
        {
            Destroy(localCamera.gameObject);
        }

        localCamera = Instantiate(cameraPrefab, transform);

        currentAvatar.GunSpawnLocation.SetParent(localCamera.transform);

        localCamera.gameObject.SetActive(true);

        fusionCameraController.Initialize(localCamera, transform);
        fusionVoxelMeshController.Initialize(this);
    }

    private void SpawnGun() 
    {
        fusionGunController.Pause();

        if (gun)
        {
            Destroy(gun.gameObject);
        }

        gun = Instantiate(gunPrefab, currentAvatar.GunSpawnLocation);

        fusionGunController.Initialize(gun);       
    }

    private void OnCurrentAvatarTypeChanged()
    {
        ChangeAvatar(CurrentAvatarType).Forget();
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
