using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.Build.Reporting;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public const int MaxPlayers = 4;

    /// <summary>
    /// <list type="bullet">
    ///<item>Waiting = waiting for players to connect</item>
    /// <item>Preparing = Start Phase where players place initial Settlements</item>
    /// <item>Playing = Normal Game Phase</item>
    /// </list>
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
    private readonly NetworkVariable<bool> _hasThrownDice = new();
    private readonly NetworkVariable<byte> _roundNumber = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (State == GameState.Preparing)
        {
            HandleInitialPlacement();
        }
    }

    public override void OnNetworkSpawn()
    {
        ConnectionNotificationManager.Instance.OnClientConnectionNotification += OnClientConnectionStatusChange;
    }

    public bool IsMyTurn()
    {
        if (State == GameState.Waiting || !NetworkManager.Singleton)
            return false;
        return _playerIds.IndexOf(NetworkManager.Singleton.LocalClientId) == _playerTurn.Value;
    }

    public bool PlaceSettlement(Settlement settlement)
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        BuySettlementRpc(NetworkManager.Singleton.LocalClientId, settlement.Id);
        return settlement.CanBeBuildBy(clientId);
    }

    public bool PlaceStreet(Street street)
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        BuyStreetRpc(clientId, street.Id);
        return street.CanBeBuildBy(clientId);
    }

    [Rpc(SendTo.Authority)]
    private void BuySettlementRpc(ulong clientId, int settlementId)
    {
        var settlement = Settlement.AllSettlements[settlementId];
        if (settlement.CanBeBuildBy(clientId))
        {
            settlement.Build(clientId);
        }
    }

    [Rpc(SendTo.Authority)]
    private void BuyStreetRpc(ulong clientId, int streetId)
    {
        var street = Street.AllStreets[streetId];
        if (street.CanBeBuildBy(clientId))
        {
            street.SetOwner(clientId);
        }
    }

    public void FinishTurn()
    {
        FinishTurnRpc();
    }

    [Rpc(SendTo.Authority)]
    private void FinishTurnRpc()
    {
        if (State != GameState.Playing) return;
        _playerTurn.Value = (byte)((_playerTurn.Value + 1) % PlayerCount);
        _hasThrownDice.Value = false;
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void DiceResultRpc(int diceOne, int diceTwo)
    {
        //  show dice roll animation whatever
    }

    public void StartGame()
    {
        _gameState.Value = (byte)GameState.Preparing;
        _roundNumber.Value = 1;
    }

    private void HandleInitialPlacement()
    {
        if (!IsMyTurn()) return;
        BuildManager.SelectBuildingType(Player.LocalPlayer.VictoryPoints < _roundNumber.Value
            ? BuildManager.BuildType.Settlement
            : BuildManager.BuildType.Street);
    }
    
    private void OnClientConnectionStatusChange(ulong clientId,
        ConnectionNotificationManager.ConnectionStatus connectionStatus)
    {
        if (!NetworkManager.Singleton.IsHost)
            return;
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
}