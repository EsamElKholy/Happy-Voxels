using Cysharp.Threading.Tasks;
using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Networked]
    public int PlayerIndex { get; private set; } = -1;
   
    private PlayerAvatar currentAvatar;
    private SpawnLocationManager spawnLocationManager;
    
    public PlayerAvatar CurrentAvatar { get { return currentAvatar; } }

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

            Debug.LogError($"CurrentAvatarType: {CurrentAvatarType}, State {HasStateAuthority}");
        }
        else
        {
            ChangeAvatar(CurrentAvatarType);
            Debug.LogError($"CurrentAvatarType: {CurrentAvatarType}, State {HasStateAuthority}");
            Debug.LogError($"Player id {PlayerIndex}, State {HasStateAuthority}");
        }
    }

    public void ChangeAvatar(AvatarType avatarType) 
    {
        if (avatarType == AvatarType.NONE)
        {
            return;
        }

        int index = avatarPrefabs.FindIndex(x => x.GetComponent<PlayerAvatar>().AvatarType == avatarType);
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
