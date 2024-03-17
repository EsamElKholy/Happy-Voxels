using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_FusionPlayer : MonoBehaviour
{
#if UNITY_EDITOR

    [SerializeField]
    private FusionPlayer fusionPlayer;

    [SerializeField, OnValueChanged(nameof(AvatarTypeChanged))]
    private AvatarType avatarType;

    
    private void Start()
    {
        avatarType = fusionPlayer.CurrentAvatarType;

        //if (fusionPlayer.HasStateAuthority)
        //{
        //    ChangeAfterDelay().Forget();

        //}
    }

    private async UniTask ChangeAfterDelay() 
    {
        await UniTask.WaitForSeconds(1);
        avatarType = fusionPlayer.CurrentAvatarType;

        fusionPlayer.Debug_ChangeDefaultAvatarType(avatarType:AvatarType.Default);
        await UniTask.DelayFrame(2);
        //List<AvatarType> avatarTypes = new List<AvatarType>() { AvatarType.Red, AvatarType.Green, AvatarType.Blue };
        //fusionPlayer.Debug_ChangeDefaultAvatarType(avatarTypes[UnityEngine.Random.Range(0, avatarTypes.Count)]);
    }

    private void AvatarTypeChanged()
    {
        fusionPlayer.Debug_ChangeDefaultAvatarType(avatarType);
    }

#endif
}
