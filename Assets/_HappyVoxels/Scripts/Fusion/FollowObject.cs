using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    [SerializeField]
    private Transform targetToFollow;

    public void SetTarget(Transform target) 
    {
        targetToFollow = target;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (targetToFollow)
        {
            transform.position = targetToFollow.position;
            transform.rotation = targetToFollow.rotation;
        }
    }
}
