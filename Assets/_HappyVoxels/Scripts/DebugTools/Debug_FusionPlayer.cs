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

        ChangeAfterDelay().Forget();
    }

    private async UniTask ChangeAfterDelay() 
    {
        await UniTask.WaitForSeconds(0.5f);
        avatarType = fusionPlayer.CurrentAvatarType;

        fusionPlayer.Debug_ChangeDefaultAvatarType(avatarType:AvatarType.Default);
        await UniTask.DelayFrame(1);
        fusionPlayer.Debug_ChangeDefaultAvatarType(avatarType);
    }

    private void AvatarTypeChanged()
    {
        fusionPlayer.Debug_ChangeDefaultAvatarType(avatarType);
    }

#endif
}
