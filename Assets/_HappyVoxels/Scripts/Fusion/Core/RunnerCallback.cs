using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunnerCallback : MonoBehaviour, INetworkRunnerCallbacks
{
    public static Action<NetworkRunner> OnConnectedToServerCallback;    
    public static Action<NetworkRunner> OnDisconnectedFromServerCallback;    
    public static Action<PlayerRef> OnPlayerJoinedSession;    
    public static Action<PlayerRef> OnPlayerLeftSession;
    public static Action<NetworkRunner, NetworkInput> OnPlayerInput;
    public virtual void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnConnectedToServer: {runner}");
        OnConnectedToServerCallback?.Invoke(runner);
    }

    public virtual void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnConnectFailed: NotImplemented");
    }

    public virtual void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnConnectRequest: NotImplemented");
    }

    public virtual void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnCustomAuthenticationResponse: NotImplemented");
    }

    public virtual void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnDisconnectedFromServer: {reason.ToString()}");
        OnDisconnectedFromServerCallback?.Invoke(runner);
    }

    public virtual void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnHostMigration: NotImplemented");
    }

    public virtual void OnInput(NetworkRunner runner, NetworkInput input)
    {
        OnPlayerInput?.Invoke(runner, input);
    }

    public virtual void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public virtual void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnObjectEnterAOI: NotImplemented");
    }

    public virtual void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnObjectExitAOI: NotImplemented");
    }

    public virtual void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnPlayerJoined: {player}");
        OnPlayerJoinedSession?.Invoke(player);
    }

    public virtual void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnPlayerLeft: {player}");
        OnPlayerLeftSession?.Invoke(player);
    }

    public virtual void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnReliableDataProgress: NotImplemented");
    }

    public virtual void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnReliableDataReceived: NotImplemented");
    }

    public virtual void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnSceneLoadDone: NotImplemented");
    }

    public virtual void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnSceneLoadStart: NotImplemented");
    }

    public virtual void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnSessionListUpdated: NotImplemented");
    }

    public virtual void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.LogError($"Fusion: RunnerCallbacks: OnShutdown: Reason = {shutdownReason}");
    }

    public virtual void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        Debug.LogWarning($"Fusion: RunnerCallbacks: OnUserSimulationMessage: NotImplemented");
    }
}
