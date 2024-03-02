using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class FusionPlayer : NetworkBehaviour
{  
    [SerializeField]
    private List<PlayerAvatar> avatarPrefabs = new();
    [SerializeField]
    private AvatarType defaultAvatarType;
    [SerializeField]
    private PlayerController playerControllerPrefab;

    [Networked, OnChangedRender(nameof(OnCurrentAvatarTypeChanged))]
    private AvatarType CurrentAvatarType { get; set; } = AvatarType.NONE;
   
    private PlayerController playerController;
    private PlayerAvatar currentAvatar;

    public override void Spawned()
    {
        base.Spawned();
               
        if (HasStateAuthority)
        {
            SpawnPlayerController();
            CurrentAvatarType = defaultAvatarType;
        }
    }

    private void SpawnPlayerController() 
    {
        var controllerObj = Instantiate(playerControllerPrefab, transform);
        playerController = controllerObj.GetComponent<PlayerController>();
    }

    public void ChangeAvatar(AvatarType avatarType) 
    {
        int index = avatarPrefabs.FindIndex(x => x.GetComponent<PlayerAvatar>().AvatarType == avatarType);
        if (index != -1)
        {
            if (currentAvatar)
            {
                Destroy(currentAvatar.gameObject);
            }

            var avatar = Instantiate(avatarPrefabs[index], transform);

            avatar.transform.localPosition = Vector3.zero;
            avatar.transform.localRotation = Quaternion.identity;
            var playerAvatar = avatar.GetComponent<PlayerAvatar>();
            currentAvatar = playerAvatar;

            if (HasStateAuthority)
            {
                if (playerAvatar)
                {
                    playerAvatar.SetFollowTarget(playerController.transform);
                }
            }
        }
    }

    private void OnCurrentAvatarTypeChanged()
    {
        ChangeAvatar(CurrentAvatarType);
    }

    private void OnValidate()
    {
        if (Object && Object.IsValid && CurrentAvatarType != defaultAvatarType)
        {
            CurrentAvatarType = defaultAvatarType;
        }
    }
}
