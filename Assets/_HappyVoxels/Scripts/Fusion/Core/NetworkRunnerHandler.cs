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
    [ScenePath]
    private string sceneToStart;

    private NetworkRunner networkRunner;
    private Action<NetworkRunner> onGameStarted;

    public void StartGame() 
    {
        SceneLoader.OnSceneLoaded += SceneLoaded;
        onGameStarted += GameStarted;
        SceneLoader.LoadScene(sceneToStart, false);
    }

    public void Exit() 
    {
        if (networkRunner)
        {
            networkRunner.Shutdown();
        }

        Application.Quit();
    }

    protected virtual async UniTask StartNetworkRunner(NetworkRunner runner, GameMode gameMode, NetAddress netAddress, Action<NetworkRunner> started) 
    {
        runner.ProvideInput = true;

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = gameMode,
            Address = netAddress,
            Scene = null,
            SessionName = sceneToStart,
            OnGameStarted = started,
            SceneManager = null,
            PlayerCount = 4,
            IsVisible = true,
            IsOpen = true,
        });

        if (!result.Ok)
        {
            Debug.LogError($"Failed to start game, result is {result.ErrorMessage}");
        }
    }

    private void SceneLoaded()
    {
        var scene = SceneManager.GetSceneByName(sceneToStart);
        networkRunner = Instantiate(networkRunnerPrefab);

        StartNetworkRunner(networkRunner, GameMode.Shared, NetAddress.Any(), onGameStarted).Forget();
    }

    private void GameStarted(NetworkRunner networkRunner)
    {
        onGameStarted -= GameStarted;

        SingletonInterface.SingletonLocator.GameUIManager.FadeOutCurrentUI().Forget();

        SceneLoader.OnSceneLoaded -= SceneLoaded;
    }
}
