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
    private const string AVATAR_LAYER = "AvatarBody";

    [SerializeField] 
    private AvatarType avatarType;
    [SerializeField]
    private Transform gunSpawnLocation;

    public AvatarType AvatarType { get { return avatarType; } }   
    public Transform GunSpawnLocation { get { return gunSpawnLocation; } }

    public void Initialize() 
    {       
        SetGameLayerRecursive(gameObject, AVATAR_LAYER);
    }

    private void SetGameLayerRecursive(GameObject obj, string layer)
    {
        obj.layer = LayerMask.NameToLayer(layer);
        foreach (Transform child in obj.transform)
        {
            child.gameObject.layer = LayerMask.NameToLayer(layer);

            Transform hasChildren = child.GetComponentInChildren<Transform>();
            if (hasChildren != null)
                SetGameLayerRecursive(child.gameObject, layer);

        }
    }
}
