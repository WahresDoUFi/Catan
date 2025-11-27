using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    /// <summary>
    /// Waiting = waiting for players to connect
    /// Preparing = Start Phase where players place initial Settlements
    /// Playing = Normal Game Phase
    /// </summary>
    public enum GameState
    {
        Waiting,
        Preparing,
        Playing,
    }

    public GameState State => (GameState)_gameState.Value;
    private readonly NetworkVariable<byte> _gameState = new();
    private readonly NetworkVariable<byte> _playerTurn = new();

    private readonly NetworkList<ulong> _playerIds = new();

    private void Awake()
    {
        Instance = this;
    }
    

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton.IsClient)
            return;
        ConnectionNotificationManager.Instance.OnClientConnectionNotification += OnClientConnectionStatusChange;
        for (var i = 0; i < NetworkManager.Singleton.ConnectedClientsIds.Count; i++)
        {
            _playerIds.Add(NetworkManager.Singleton.ConnectedClientsIds[i]);
        }
    }

    public bool IsMyTurn()
    {
        if (State == GameState.Waiting)
            return false;
        return true;
    }

    private void OnClientConnectionStatusChange(ulong clientId,
        ConnectionNotificationManager.ConnectionStatus connectionStatus)
    {
        if (connectionStatus == ConnectionNotificationManager.ConnectionStatus.Connected)
        {
            _playerIds.Add(clientId);
            if (State == (byte)GameState.Waiting)
            {
                if (NetworkManager.Singleton.ConnectedClientsIds.Count == 4)
                {
                    StartGame();
                }
            }
        }
    }

    private void StartGame()
    {
        _gameState.Value = (byte)GameState.Preparing;
    }
}