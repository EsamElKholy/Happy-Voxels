using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkRunnerHandler : MonoBehaviour
{
    [SerializeField]
    private NetworkRunner networkRunnerPrefab;

    [SerializeField]
    private string sceneToStart;

    private NetworkRunner networkRunner;

    private void Start()
    {
        StartGame();
    }

    private void StartGame() 
    {
        networkRunner = Instantiate(networkRunnerPrefab);

        StartNetworkRunner(networkRunner, GameMode.Shared, NetAddress.Any(), SceneRef.FromIndex(SceneManager.GetSceneByName(sceneToStart).buildIndex), null).Forget();
    }

    protected virtual async UniTask StartNetworkRunner(NetworkRunner runner, GameMode gameMode, NetAddress netAddress, SceneRef scene, Action<NetworkRunner> started) 
    {
        var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();

        if (sceneManager == null) 
        {
            runner.AddComponent<NetworkSceneManagerDefault>();
        }

        runner.ProvideInput = true;

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = gameMode,
            Address = netAddress,
            Scene = scene,
            SessionName = "Lobby",
            OnGameStarted = started,
            SceneManager = sceneManager,
            PlayerCount = 4,
            IsVisible = true,
            IsOpen = true,
        });

        if (!result.Ok)
        {
            Debug.LogError($"Failed to start game, result is {result.ErrorMessage}");
        }
    }
}
