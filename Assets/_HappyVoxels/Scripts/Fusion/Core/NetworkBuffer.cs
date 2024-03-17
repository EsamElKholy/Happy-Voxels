using Fusion;
using System;

using UnityEngine;

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
            if (arg is float @float)
            {
                index = Record(@float, index);
            }            
            else
            {
                Debug.LogError($"Unsupported parameter.{arg.GetType()}");
            }
        }
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

    public float GetFloat(int index)
    {
        return BitConverter.ToSingle(Buffer.ToArray(), index);
    }
}
