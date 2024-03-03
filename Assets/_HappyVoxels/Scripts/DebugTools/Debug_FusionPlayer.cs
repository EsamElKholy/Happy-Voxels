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
    }

    private void AvatarTypeChanged()
    {
        fusionPlayer.Debug_ChangeDefaultAvatarType(avatarType);
    }

#endif
}
