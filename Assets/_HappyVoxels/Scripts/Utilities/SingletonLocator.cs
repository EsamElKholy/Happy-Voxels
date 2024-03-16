using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-150)]
public class SingletonLocator : MonoBehaviour
{
    public static SingletonLocator Instance { get; private set; }

    private Dictionary<Type, Component> instances = new Dictionary<Type, Component>();
    private Dictionary<Type, bool> instanceExist = new Dictionary<Type, bool>();

    public GameUIManager GameUIManager => GetInstance<GameUIManager>();
    public NetworkRunnerHandler NetworkRunner => GetInstance<NetworkRunnerHandler>();
    public SpawnLocationManager SpawnLocationManager => GetInstance<SpawnLocationManager>();
    public FusionPlayerManager FusionPlayerManager => GetInstance<FusionPlayerManager>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        ClearInstanceExistDictionary();

        Instance = this;
    }

    public bool HasInstance<T>() where T : Component
    {
        bool SearchInstanceExist()
        {
            // Find the generic instance
            T instance = FindAnyObjectByType<T>();

            bool isExist = instance != null;
            instanceExist.Add(typeof(T), isExist);

            // Caching the instance found if was not already
            if (isExist && !instances.ContainsKey(typeof(T)))
                instances.Add(typeof(T), instance);

            return isExist;
        }

        return instanceExist.TryGetValue(typeof(T), out bool exist) ? exist : SearchInstanceExist();
    }

    private T GetInstance<T>() where T : Component
    {
        T SearchInstance()
        {
            // Find the generic instance
            T instance = FindAnyObjectByType<T>();

            if (instance != null)
            {
                instances.Add(typeof(T), instance);
            }

            return instance;
        }

        if (instances.TryGetValue(typeof(T), out Component c))
        {
            if (c == null)
            {
                instances.Remove(typeof(T));
                return SearchInstance();
            }

            return (T)c;
        }
        else
        {
            return SearchInstance();
        }
    }

    private void ClearInstanceExistDictionary()
    {
        instanceExist.Clear();
    }

}
