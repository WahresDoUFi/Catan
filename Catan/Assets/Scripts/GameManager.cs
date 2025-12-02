using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

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
    
    [SerializeField]
    private Color[] playerColors;
    
    private readonly NetworkVariable<byte> _gameState = new();
    private readonly NetworkVariable<byte> _playerTurn = new();
    private readonly NetworkList<ulong> _playerIds = new();
    private readonly NetworkVariable<bool> _hasThrownDice = new();
    private readonly NetworkVariable<byte> _roundNumber = new();
    private static readonly NetworkVariable<int> Seed = new();


    private void Awake()
    {
        Seed.Value = new Random().Next(0, int.MaxValue);
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
        Street.AllStreets.Sort((s1, s2) => s1.transform.GetSiblingIndex().CompareTo(s2.transform.GetSiblingIndex()));
        Settlement.AllSettlements.Sort((s1, s2) => s1.transform.GetSiblingIndex().CompareTo(s2.transform.GetSiblingIndex()));
        if (HasAuthority)
        {
            foreach (var playerId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                _playerIds.Add(playerId);
            }
        }
        ConnectionNotificationManager.Instance.OnClientConnectionNotification += OnClientConnectionStatusChange;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        _gameState.OnValueChanged += (_, _) => BuildManager.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        ConnectionNotificationManager.Instance.OnClientConnectionNotification -= OnClientConnectionStatusChange;
    }

    public bool IsMyTurn()
    {
        if (State == GameState.Waiting || !NetworkManager.Singleton)
            return false;
        return _playerIds.IndexOf(NetworkManager.Singleton.LocalClientId) == _playerTurn.Value;
    }

    public Color GetPlayerColor(ulong playerId)
    {
        return playerColors[_playerIds.IndexOf(playerId)];
    }

    public bool PlaceSettlement(Settlement settlement)
    {
        if (!settlement) return false;
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        BuySettlementRpc(NetworkManager.Singleton.LocalClientId, settlement.Id);
        return settlement.CanBeBuildBy(clientId);
    }

    public bool PlaceStreet(Street street)
    {
        if (!street) return false;
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
            if (State == GameState.Preparing)
            {
                NextTurn();
                if (_roundNumber.Value > 2)
                {
                    FinishStartingPhase();
                }
            }
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
        NextTurn();
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
    private void DiceResultRpc(int diceOne, int diceTwo)
    {
        //  show dice roll animation whatever
    }

    public void StartGame()
    {
        var one = DiceRoll.GetResult(Seed.Value).Item1;
        var two = DiceRoll.GetResult(Seed.Value).Item2;
        Debug.Log($"{one} + {two}");
        _gameState.Value = (byte)GameState.Preparing;
        _roundNumber.Value = 1;
    }

    private void NextTurn()
    {
        _hasThrownDice.Value = false;
        _playerTurn.Value = (byte)((_playerTurn.Value + 1) % PlayerCount);
        if (_playerTurn.Value == 0)
            _roundNumber.Value += 1;
    }

    private void FinishStartingPhase()
    {
        _gameState.Value = (byte)GameState.Playing;
        foreach (var settlement in Settlement.AllSettlements)
        {
            if (!settlement.IsOccupied) continue;
            foreach (var tile in settlement.FindNeighboringTiles())
            {
                tile.Discover();
                Player.GetPlayerById(settlement.Owner).UpdateResources(tile.TileType, 1);
            }
        }
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
        switch (connectionStatus)
        {
            case ConnectionNotificationManager.ConnectionStatus.Connected:
            {
                _playerIds.Add(clientId);
                if (State == (byte)GameState.Waiting)
                {
                    if (NetworkManager.Singleton.ConnectedClientsIds.Count == 4)
                    {
                        StartGame();
                    }
                }

                break;
            }
            case ConnectionNotificationManager.ConnectionStatus.Disconnected:
                _playerIds.Remove(clientId);
                break;
        }
    }

    private void OnClientStopped(bool isHost)
    {
        SceneManager.LoadScene(0);
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
    }
}