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
    private FusionPlayerController fusionPlayerController;

    [Networked, OnChangedRender(nameof(OnCurrentAvatarTypeChanged))]
    private AvatarType CurrentAvatarType { get; set; } = AvatarType.NONE;
   
    private PlayerAvatar currentAvatar;

    public override void Spawned()
    {
        base.Spawned();
               
        if (HasInputAuthority)
        {
            fusionPlayerController.Initialize(this);
            CurrentAvatarType = defaultAvatarType;
        }
        else
        {
            ChangeAvatar(CurrentAvatarType);
        }
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

            //if (HasStateAuthority)
            //{
            //    if (playerAvatar)
            //    {
            //        playerAvatar.SetFollowTarget(fusionPlayerController.transform);
            //    }
            //}
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (Object && Object.IsValid && CurrentAvatarType != defaultAvatarType)
        {
            RPC_ChangeAvatar(defaultAvatarType);
        }
    }
#endif

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ChangeAvatar(AvatarType avatarType) 
    {
        CurrentAvatarType = avatarType;
    }

    private void OnCurrentAvatarTypeChanged()
    {
        ChangeAvatar(CurrentAvatarType);
    }
}
