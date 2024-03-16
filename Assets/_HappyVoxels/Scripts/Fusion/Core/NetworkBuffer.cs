using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class NetworkBuffer<T> : INetworkStruct
{
    public const int BufferLength = 128;

    [Networked, Capacity(BufferLength)]
    private NetworkArray<byte> Buffer => default;

    public byte[] Data => Buffer.ToArray();
    public int Count { get; private set; }
    private Type currentType;

    public NetworkBuffer(params T[] args)
    {
        currentType = typeof(T);
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

    public int Record(T value, int startIndex) 
    {
        if (value is bool @bool)
        {
            return Record(@bool, startIndex);
        }
        else if (value is int @int)
        {
            return Record(@int, startIndex);
        }
        else if (value is float @float)
        {
            return Record(@float, startIndex);
        }
        else
        {
            Debug.LogError($"Unsupported parameter.{value.GetType()}");
        }

        return -1;
    }

    private int Record(bool value, int startIndex)
    {
        var bytes = BitConverter.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            Buffer.Set(startIndex + i, bytes[i]);
        }
        Count++; 
        return startIndex + bytes.Length;
    }

    private int Record(int value, int startIndex)
    {
        var bytes = BitConverter.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            Buffer.Set(startIndex + i, bytes[i]);
        }
        return startIndex + bytes.Length;
    }

    private int Record(float value, int startIndex)
    {
        var bytes = BitConverter.GetBytes(value);
        for (var i = 0; i < bytes.Length; i++)
        {
            Buffer.Set(startIndex + i, bytes[i]);
        }
        return startIndex + bytes.Length;
    }

    public object GetValue(int index) 
    {
        if (currentType == typeof(bool))
        {
            return GetBool(index);
        }
        else if (currentType == typeof(int))
        {
            return GetInt(index);
        }
        else if (currentType == typeof(float))
        {
            return GetFloat(index);
        }

        return null;
    }

    private bool GetBool(int index)
    {
        return BitConverter.ToBoolean(Buffer.ToArray(), index);
    }

    private int GetInt(int index)
    {
        return BitConverter.ToInt32(Buffer.ToArray(), index);
    }

    private float GetFloat(int index)
    {
        return BitConverter.ToSingle(Buffer.ToArray(), index);
    }
}
