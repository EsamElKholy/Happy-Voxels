using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterInputHandler : MonoBehaviour
{
    private bool isInitialized = false;
    private Vector2 aim;
    private Vector2 move;
    private bool isFiring = false;
    private bool isSphereEnabling = false;
    private bool isSphereDisabling = false;

    private bool changeToRed = false;
    private bool changeToGreen = false;
    private bool changeToBlue = false;

    private void OnDestroy()
    {
        if (isInitialized)
            RunnerCallback.OnPlayerInput -= OnPlayerInput;
    }

    public void Initialize() 
    {
        isInitialized = true;
        RunnerCallback.OnPlayerInput += OnPlayerInput;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!isInitialized) 
        {
            return;
        }

        aim.x = Input.GetAxis("Mouse X");
        aim.y = Input.GetAxis("Mouse Y") * -1;

        move.x = Input.GetAxis("Horizontal");
        move.y = Input.GetAxis("Vertical");

        if (Input.GetButton("Fire1"))
        {
            isFiring = true;
            isSphereDisabling = true;
        }

        if (Input.GetButtonUp("Fire1"))
        {
            isFiring = false;
            isSphereDisabling = false;
        }

        if (Input.GetButton("Fire2"))
        {
            isSphereEnabling = true;
        }

        if (Input.GetButtonUp("Fire2"))
        {
            isSphereEnabling = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            changeToRed = true;
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            changeToRed = false;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            changeToGreen = true;
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            changeToGreen = false;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            changeToBlue = true;
        }

        if (Input.GetKeyUp(KeyCode.B))
        {
            changeToBlue = false;
        }
    }

    private void OnPlayerInput(NetworkRunner runner, NetworkInput input)
    {
        input.Set(GetInputData());
    }

    public NetworkInputData GetInputData() 
    {
        NetworkInputData networkInputData = new NetworkInputData();
        
        networkInputData.mouseAim = aim;
        networkInputData.movementInput = move;
        networkInputData.isFiring = isFiring;
        networkInputData.isSphereEnabling = isSphereEnabling;
        networkInputData.isSphereDisabling = isSphereDisabling;

        networkInputData.changeToRed = changeToRed;
        networkInputData.changeToGreen = changeToGreen;
        networkInputData.changeToBlue = changeToBlue;

        return networkInputData;
    }
}
