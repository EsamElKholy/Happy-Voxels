using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum AvatarType
{
    Default,
    Red,
    Green,
    Blue,
    NONE
};

public class PlayerAvatar : MonoBehaviour
{
    [SerializeField] 
    private AvatarType avatarType;

    private FollowObject follow;

    public AvatarType AvatarType { get { return avatarType; } }

    private void Awake()
    {
        Initialize();
    }

    private void Initialize() 
    {
        follow = GetComponent<FollowObject>();

    }

    public void SetFollowTarget(Transform target) 
    {
        if (!follow)
        {
            Initialize();
        }

        if (follow)
        {
            follow.SetTarget(target);
        }
    }
}
