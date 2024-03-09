using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void StartGame() 
    {
        SingletonInterface.SingletonLocator.NetworkRunner.StartGame();
    }

    public void Exit() 
    {
        SingletonInterface.SingletonLocator.NetworkRunner.Exit();
    }
}
