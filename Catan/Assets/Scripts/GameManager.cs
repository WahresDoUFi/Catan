using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public const int MaxPlayers = 4;

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
    public int PlayerCount => _playerIds.Count;
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
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            _playerIds.Add(clientId);
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