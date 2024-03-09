using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SingletonInterface 
{
    public static SingletonLocator SingletonLocator => SingletonLocator.Instance;

}
