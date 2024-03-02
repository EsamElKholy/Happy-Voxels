using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnLocation : NetworkBehaviour
{
    [Networked]
    public bool IsUsed { get; set; }
}
