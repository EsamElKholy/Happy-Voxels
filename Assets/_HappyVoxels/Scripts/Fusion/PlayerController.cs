using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private CharacterController characterController;
    [SerializeField]
    private float playerSpeed = 2;

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * Time.fixedDeltaTime * playerSpeed;

        characterController.Move(move);

        if (move != Vector3.zero)
        {
            transform.forward = move;
        }
    }
}
