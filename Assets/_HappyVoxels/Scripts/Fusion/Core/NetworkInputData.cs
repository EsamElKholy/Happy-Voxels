using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
    public Vector2 mouseAim;
    
    public bool isFiring;

    public bool isSphereEnabling;
    public bool isSphereDisabling;

    public bool changeToRed;
    public bool changeToGreen;
    public bool changeToBlue;
}
