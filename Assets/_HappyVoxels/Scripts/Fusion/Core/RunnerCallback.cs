using Fusion;
using Fusion.Sockets;
using System;
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
        OnConnectedToServerCallback?.Invoke(runner);
    }

    public virtual void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public virtual void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public virtual void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public virtual void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        OnDisconnectedFromServerCallback?.Invoke(runner);
    }

    public virtual void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
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
    }

    public virtual void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public virtual void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerJoinedSession?.Invoke(player);
    }

    public virtual void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        OnPlayerLeftSession?.Invoke(player);
    }

    public virtual void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public virtual void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
    {
    }

    public virtual void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public virtual void OnSceneLoadStart(NetworkRunner runner)
    {
    }

    public virtual void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public virtual void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    // Still required by INetworkRunnerCallbacks, but obsolete since Fusion 2.1 and never called.
#pragma warning disable CS0618
    public virtual void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }
#pragma warning restore CS0618
}
