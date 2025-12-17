using System.Collections.Generic;
using System.Linq;
using Misc;
using UI;
using UI.Trade;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using User;
using Random = System.Random;

namespace GamePlay
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;
        public const int MaxPlayers = 4;
        public const int MaxCardsOnBandit = 6;
        private const int VictoryPointsTarget = 7;

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
        public bool DiceThrown => _hasThrownDice.Value;
        public int Seed => _seed.Value;
        public ulong ActivePlayer => _playerIds[_playerTurn.Value];
        public bool IsGameOver => gameOverScreen.gameObject.activeSelf;
        public bool CardLimitActive => _cardsToDiscard.AsNativeArray().Any(cards => cards > 0);
        public int CardsToDiscard => _cardsToDiscard[_playerIds.IndexOf(NetworkManager.LocalClientId)];

        [SerializeField] private Color[] playerColors;
        [SerializeField] private GameOverScreen gameOverScreen;

        private readonly NetworkVariable<byte> _gameState = new();
        private readonly NetworkVariable<byte> _playerTurn = new();
        private readonly NetworkList<ulong> _playerIds = new();
        private readonly NetworkVariable<bool> _hasThrownDice = new();
        private readonly NetworkVariable<byte> _roundNumber = new();
        private readonly NetworkVariable<int> _seed = new();
        private readonly NetworkTradeInfoVariable _playerTrades = new();
        private readonly NetworkList<byte> _cardsToDiscard = new();

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
            Street.AllStreets.Sort((s1, s2) =>
                s1.transform.GetSiblingIndex().CompareTo(s2.transform.GetSiblingIndex()));
            Settlement.AllSettlements.Sort((s1, s2) =>
                s1.transform.GetSiblingIndex().CompareTo(s2.transform.GetSiblingIndex()));
            if (HasAuthority)
            {
                _seed.Value = new Random().Next(0, int.MaxValue);
                foreach (ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    _playerIds.Add(playerId);
                    _cardsToDiscard.Add(0);
                }
            }
            foreach (ulong playerId in _playerIds)
            {
                PlayerCardList.AddPlayerCard(Player.GetPlayerById(playerId));
            }
            ConnectionNotificationManager.Instance.OnClientConnectionNotification += OnClientConnectionStatusChange;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            _gameState.OnValueChanged += (_, _) => GameStateChange();
            _playerTurn.OnValueChanged += (_, _) => PlayerTurnChange();
            _hasThrownDice.OnValueChanged += HasThrownDiceChange;
            _playerTrades.OnValueChanged += AvailableTradesMenu.UpdateAvailableTrades;
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

        public IEnumerable<ulong> GetPlayerIds()
        {
            foreach (ulong clientId in _playerIds)
            {
                yield return clientId;
            }
        }

        public bool CanThrowDice()
        {
            if (State != GameState.Playing) return false;
            return IsMyTurn() && !DiceThrown;
        }

        public void MarkDiceStable()
        {
            if (!NetworkManager.Singleton.IsHost) return;
            var result = DiceRoll.GetResult(_seed.Value);
            GrantResources(result.first + result.second);
            _seed.Value = new Random().Next(0, int.MaxValue);
            _hasThrownDice.Value = true;
        }

        public Color GetPlayerColor(ulong playerId)
        {
            return playerColors[_playerIds.IndexOf(playerId)];
        }

        public bool PlaceSettlement(Settlement settlement)
        {
            if (!settlement) return false;
            if (State == GameState.Playing &&
                !Player.LocalPlayer.HasResources(BuildManager.GetCostsForBuilding(BuildManager.BuildType.Settlement)))
                return false;
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            BuySettlementRpc(NetworkManager.Singleton.LocalClientId, settlement.Id);
            return settlement.CanBeBuildBy(clientId);
        }

        public bool PlaceStreet(Street street)
        {
            if (!street) return false;
            if (State == GameState.Playing &&
                !Player.LocalPlayer.HasResources(BuildManager.GetCostsForBuilding(BuildManager.BuildType.Street)))
                return false;
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            BuyStreetRpc(clientId, street.Id);
            return street.CanBeBuildBy(clientId);
        }

        [Rpc(SendTo.Authority)]
        private void BuySettlementRpc(ulong clientId, int settlementId)
        {
            var settlement = Settlement.AllSettlements[settlementId];
            if (!settlement.CanBeBuildBy(clientId)) return;

            if (State == GameState.Playing)
            {
                var player = Player.GetPlayerById(clientId);
                var costs = BuildManager.GetCostsForBuilding(BuildManager.BuildType.Settlement);
                if (!player.HasResources(costs))
                    return;
                foreach (var cost in costs)
                {
                    player.RemoveResources(cost.resource, cost.amount);
                }
            }

            settlement.Build(clientId);
            if (State != GameState.Playing) return;
            foreach (var tile in settlement.FindNeighboringTiles())
            {
                tile.Discover();
            }
        }

        [Rpc(SendTo.Authority)]
        private void BuyStreetRpc(ulong clientId, int streetId)
        {
            var street = Street.AllStreets[streetId];
            if (!street.CanBeBuildBy(clientId)) return;

            if (State == GameState.Playing)
            {
                var player = Player.GetPlayerById(clientId);
                var costs = BuildManager.GetCostsForBuilding(BuildManager.BuildType.Street);
                if (!player.HasResources(costs)) return;
                foreach (var cost in costs)
                {
                    player.RemoveResources(cost.resource, cost.amount);
                }
            }

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

        public void TradeResources(Tile give, Tile get)
        {
            TradeResourcesRpc(NetworkManager.LocalClientId, (int)give, (int)get);
        }

        [Rpc(SendTo.Authority)]
        private void TradeResourcesRpc(ulong clientId, int give, int get)
        {
            if (_playerIds.IndexOf(clientId) != _playerTurn.Value) return;
            var player = Player.GetPlayerById(clientId);
            var costs = new BuildManager.ResourceCosts[]
                { new BuildManager.ResourceCosts() { amount = 4, resource = (Tile)give } };
            if (!player.HasResources(costs)) return;
            player.RemoveResources((Tile)give, 4);
            player.AddResources((Tile)get, 1);
        }

        public void FinishTurn()
        {
            BuildManager.SetActive(false);
            if (VictoryPoints.CalculateVictoryPoints(NetworkManager.Singleton.LocalClientId) >= VictoryPointsTarget)
            {
                ShowGameOverClientRpc(NetworkManager.Singleton.LocalClientId);
            }
            else
            {
                FinishTurnRpc();
            }
        }

        [ClientRpc]
        private void ShowGameOverClientRpc(ulong winnerClientId)
        {
            gameOverScreen.ShowGameOverScreen(winnerClientId, NetworkManager.Singleton.LocalClientId);
        }

        [Rpc(SendTo.Authority)]
        private void FinishTurnRpc(RpcParams rpcParams = default)
        {
            if (State != GameState.Playing) return;
            if (ActivePlayer != rpcParams.Receive.SenderClientId) return;
            if (!DiceThrown) return;
            NextTurn();
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void DiceResultRpc(int diceOne, int diceTwo)
        {
            //  show dice roll animation whatever
        }

        public TradeInfo[] GetAvailableTrades()
        {
            var result = new List<TradeInfo>();
            var localClientId = NetworkManager.Singleton.LocalClientId;
            foreach (var trade in _playerTrades.Trades)
            {
                if (trade.ReceiverId == localClientId)
                    result.Add(trade);
            }
            return result.ToArray();
        }

        public int GetTradeId(TradeInfo trade)
        {
            return _playerTrades.Trades.IndexOf(trade);
        }

        public void CreateTrade(TradeInfo tradeInfo)
        {
            CreateTradeRpc(tradeInfo);
        }

        public void AcceptTrade(int tradeId)
        {
            AcceptTradeRpc(tradeId);
        }

        [Rpc(SendTo.Authority)]
        private void AcceptTradeRpc(int tradeId, RpcParams rpcParams = default)
        {
            if (tradeId < 0 || tradeId >= _playerTrades.Trades.Count) return;
            var trade = _playerTrades.Trades[tradeId];
            if (trade.ReceiverId != rpcParams.Receive.SenderClientId) return;
            var receiver = Player.GetPlayerById(trade.ReceiverId);
            var sender = Player.GetPlayerById(trade.SenderId);
            if (!receiver.HasResources(trade.ReceiveResources)) return;
            if (!sender.HasResources(trade.SendResources)) return;
            foreach (var resource in trade.ReceiveResources)
            {
                receiver.RemoveResources(resource.resource, resource.amount);
                sender.AddResources(resource.resource, resource.amount);
            }

            foreach (var resource in trade.SendResources)
            {
                sender.RemoveResources(resource.resource, resource.amount);
                receiver.AddResources(resource.resource, resource.amount);
            }

            _playerTrades.RemoveTrade(trade);
        }

        [Rpc(SendTo.Authority)]
        private void CreateTradeRpc(TradeInfo tradeInfo, RpcParams rpcParams = default)
        {
            if (tradeInfo.SenderId != rpcParams.Receive.SenderClientId) return;
            //  only trades with the player whose turn it is are allowed
            if (tradeInfo.ReceiverId == ActivePlayer || tradeInfo.SenderId == ActivePlayer)
                _playerTrades.AddTrade(tradeInfo);
        }

        public void StartGame()
        {
            _gameState.Value = (byte)GameState.Preparing;
            _roundNumber.Value = 1;
        }

        public void DiscardResource(Tile resource)
        {
            DiscardResourceRpc(resource);
        }

        [Rpc(SendTo.Authority)]
        private void DiscardResourceRpc(Tile resource, RpcParams rpcParams = default)
        {
            var playerIndex = _playerIds.IndexOf(rpcParams.Receive.SenderClientId);
            if (_cardsToDiscard[playerIndex] == 0) return;
            var player = Player.GetPlayerById(rpcParams.Receive.SenderClientId);
            player.RemoveResources(resource, 1);
            _cardsToDiscard[playerIndex]--;
        }

        private void NextTurn()
        {
            int victoryPoints = VictoryPoints.CalculateVictoryPoints(ActivePlayer);
            if (victoryPoints >= 7)
            {
                //Add Game Over!
            }

            _hasThrownDice.Value = false;
            _playerTrades.Clear();
            _playerTurn.Value = (byte)((_playerTurn.Value + 1) % PlayerCount);
            if (_playerTurn.Value == 0)
                _roundNumber.Value += 1;

            if (PlayerCount == 1)
                PlayerTurnChange();
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
                    Player.GetPlayerById(settlement.Owner).AddResources(tile.TileType, 1);
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

        private void HasThrownDiceChange(bool previous, bool current)
        {
            DiceController.Instance.Reset();
        }

        private void GrantResources(int number)
        {
            if (number is 7)
            {
                for (var i = 0; i < _playerIds.Count; i++)
                {
                    int cardCount = Player.GetPlayerById(_playerIds[i]).ResourceCount;
                    if (cardCount > MaxCardsOnBandit)
                    {
                        _cardsToDiscard[i] = (byte)Mathf.FloorToInt(cardCount / 2f);
                    }
                }
                return;
            }
            foreach (var settlement in Settlement.AllSettlements)
            {
                if (!settlement.IsOccupied) continue;
                foreach (var tile in settlement.FindNeighboringTiles())
                {
                    if (tile.Number == number)
                    {
                        Player.GetPlayerById(settlement.Owner).AddResources(tile.TileType, settlement.Level);
                    }
                }
            }
        }

        private void GameStateChange()
        {
            BuildManager.SetActive(false);
            DiceController.Instance.Reset();
        }

        private void PlayerTurnChange()
        {
            TradeMenu.Instance.Close();
            TradeWindow.Close();
            DiceController.Instance.Reset();
            PlayerCardList.RollDice(ActivePlayer);
        }

        private void OnClientConnectionStatusChange(ulong clientId,
            ConnectionNotificationManager.ConnectionStatus connectionStatus)
        {
            if (connectionStatus == ConnectionNotificationManager.ConnectionStatus.Connected)
            {
                PlayerCardList.AddPlayerCard(Player.GetPlayerById(clientId));
            }
            else
            {
                PlayerCardList.RemovePlayerCard(clientId);
            }
            if (!NetworkManager.Singleton.IsHost)
                return;
            switch (connectionStatus)
            {
                case ConnectionNotificationManager.ConnectionStatus.Connected:
                {
                    _cardsToDiscard.Add(0);
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
                    _cardsToDiscard.RemoveAt(_playerIds.IndexOf(clientId));
                    _playerIds.Remove(clientId);
                    break;
            }
        }

        private void OnClientStopped(bool isHost)
        {
            _ = LoadingScreen.PerformTasksInOrder(
                () => SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(0)),
                SceneManager.LoadSceneAsync(0, LoadSceneMode.Additive),
                SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene()));
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        }
    }
}