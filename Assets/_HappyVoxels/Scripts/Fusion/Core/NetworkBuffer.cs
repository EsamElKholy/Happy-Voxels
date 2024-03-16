using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public struct NetworkBuffer : INetworkStruct
{
    public const int BufferLength = 128;

    [Networked, Capacity(BufferLength)]
    private NetworkArray<byte> Buffer => default;

    public byte[] Data => Buffer.ToArray();

    public NetworkBuffer(params object[] args)
    {
       
        var index = 0;
        foreach (var arg in args)
        {
            if (arg is bool @bool)
            {
                index = Record(@bool, index);
            }
            else if (arg is int @int)
            {
                index = Record(@int, index);
            }
            else if (arg is float @float)
            {
                index = Record(@float, index);
            }            
            else
            {
                Debug.LogError($"Unsupported parameter.{arg.GetType()}");
            }
        }
    }

    public int Record(bool value, int startIndex)
    {
        var bytes = BitConverter.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            Buffer.Set(startIndex + i, bytes[i]);
        }
        return startIndex + bytes.Length;
    }

    public int Record(int value, int startIndex)
    {
        var bytes = BitConverter.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            Buffer.Set(startIndex + i, bytes[i]);
        }
        return startIndex + bytes.Length;
    }

    public int Record(float value, int startIndex)
    {
        var bytes = BitConverter.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            Buffer.Set(startIndex + i, bytes[i]);
        }
        return startIndex + bytes.Length;
    }

    public bool GetBool(int index)
    {
        return BitConverter.ToBoolean(Buffer.ToArray(), index);
    }

    public int GetInt(int index)
    {
        return BitConverter.ToInt32(Buffer.ToArray(), index);
    }

    public float GetFloat(int index)
    {
        return BitConverter.ToSingle(Buffer.ToArray(), index);
    }
}
