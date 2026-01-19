using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Misc;
using UI;
using UI.DevelopmentCards;
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
        public int CardsToDiscard => _cardsToDiscard[LocalPlayerIndex];
        public bool RepositionBandit => _repositionBanditState.Value.IsBitSet(0);
        public bool CanStealResource => _repositionBanditState.Value.IsBitSet(1) && !CardLimitActive;
        private int LocalPlayerIndex => Mathf.Max(0, _playerIds.IndexOf(NetworkManager.LocalClientId));
        public event Action TurnChanged;

        [SerializeField] private Color[] playerColors;
        [SerializeField] private GameOverScreen gameOverScreen;

        private readonly NetworkVariable<byte> _gameState = new();
        private readonly NetworkVariable<byte> _playerTurn = new();
        private readonly NetworkList<ulong> _playerIds = new();
        private readonly NetworkVariable<bool> _hasThrownDice = new();
        private readonly NetworkVariable<byte> _repositionBanditState = new();
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
            HandleFreeBuildingSelection();
            if (!IsHost) return;
            if (State == GameState.Preparing)
            {
                if (!Player.GetPlayerById(ActivePlayer).HasFreeBuildings())
                {
                    NextTurn();
                    if (_roundNumber.Value > 2)
                        FinishStartingPhase();
                    else
                        GrantFreeBuildings();
                }
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
                _seed.Value = new Random().Next(int.MinValue, int.MaxValue);
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
            _playerIds.OnListChanged += PlayerIdsChange;
            _gameState.OnValueChanged += (_, _) => GameStateChange();
            _playerTurn.OnValueChanged += (_, _) => PlayerTurnChange();
            _hasThrownDice.OnValueChanged += HasThrownDiceChange;
            _playerTrades.TradeUpdated += TradeUpdated;
            _playerTrades.TradeCleared += AvailableTradesMenu.UpdateAvailableTrades;
            _repositionBanditState.OnValueChanged += RepositionBanditChange;
        }

        public override void OnNetworkDespawn()
        {
            ConnectionNotificationManager.Instance.OnClientConnectionNotification -= OnClientConnectionStatusChange;
        }

        public bool IsMyTurn()
        {
            if (State == GameState.Waiting || !NetworkManager.Singleton)
                return false;
            return LocalPlayerIndex == _playerTurn.Value;
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
            _seed.Value = new Random().Next(int.MinValue, int.MaxValue);
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
                !Player.LocalPlayer.CanAfford(BuildManager.BuildType.Settlement))
                return false;
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            BuySettlementRpc(NetworkManager.Singleton.LocalClientId, settlement.Id);
            return settlement.CanBeBuildBy(clientId);
        }

        public bool PlaceStreet(Street street)
        {
            if (!street) return false;
            if (State == GameState.Playing &&
                !Player.LocalPlayer.CanAfford(BuildManager.BuildType.Street))
                return false;
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            BuyStreetRpc(clientId, street.Id);
            return street.CanBeBuildBy(clientId);
        }

        public void BuyDevelopmentCard()
        {
            var costs = BuildManager.GetCostsForBuilding(BuildManager.BuildType.DevelopmentCard);
            if (!Player.LocalPlayer.HasResources(costs)) return;
            BuyDevelopmentCardRpc();
        }

        public void SetBanditTile(MapTile tile)
        {
            if (tile.Discovered == false) return;
            SetBanditTileRpc(tile);
        }

        [Rpc(SendTo.Authority)]
        private void SetBanditTileRpc(NetworkBehaviourReference reference, RpcParams rpcparams = default)
        {
            if (RepositionBandit == false) return;
            var senderId = rpcparams.Receive.SenderClientId;
            if (ActivePlayer != senderId) return;
            if (!reference.TryGet(out var tileObject)) return;
            var tile = tileObject.GetComponent<MapTile>();
            if (tile == null) return;
            if (tile.Discovered == false) return;
            Bandit.Instance.SetTargetTile(tile);
            _repositionBanditState.Value = _repositionBanditState.Value.SetBitNoRef(0, false);
            CheckResourceStealAbilit();
        }

        public IEnumerable<ulong> PlayersInBanditRange()
        {
            foreach (var settlement in Settlement.AllSettlements)
            {
                if (!settlement.IsOccupied) continue;
                if (settlement.Owner == ActivePlayer) continue;
                if (Player.GetPlayerById(settlement.Owner).ResourceCount == 0) continue;
                if (settlement.FindNeighboringTiles().Any(tile => tile.Blocked))
                    yield return settlement.Owner;
            }
        }

        private void CheckResourceStealAbilit()
        {
            if (!IsHost) return;
            
            _repositionBanditState.Value = _repositionBanditState.Value.SetBitNoRef(1, PlayersInBanditRange().Count() > 0);
        }

        [Rpc(SendTo.Authority)]
        private void BuyDevelopmentCardRpc(RpcParams rpcParams = default)
        {
            var player = Player.GetPlayerById(rpcParams.Receive.SenderClientId);
            var costs = BuildManager.GetCostsForBuilding(BuildManager.BuildType.DevelopmentCard);
            if (!player.HasResources(costs)) return;
            foreach (var cost in costs)
            {
                player.RemoveResources(cost.resource, cost.amount);
            }
            player.BuyDevelopmentCard(RandomDevelopmentCard.Next());
        }

        [Rpc(SendTo.Authority)]
        private void BuySettlementRpc(ulong clientId, int settlementId)
        {
            var settlement = Settlement.AllSettlements[settlementId];
            if (!settlement.CanBeBuildBy(clientId)) return;
            var player = Player.GetPlayerById(clientId);

            if (!player.CanAfford(BuildManager.BuildType.Settlement)) return;
            player.Purchase(BuildManager.BuildType.Settlement);

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
            var player = Player.GetPlayerById(clientId);

            if (!player.CanAfford(BuildManager.BuildType.Street)) return;
            player.Purchase(BuildManager.BuildType.Street);

            street.SetOwner(clientId);
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
            FinishTurnRpc();
        }

        [ClientRpc]
        private void ShowGameOverClientRpc(ulong winnerClientId)
        {
            gameOverScreen.ShowGameOverScreen(Player.GetPlayerById(winnerClientId));
        }

        [Rpc(SendTo.Authority)]
        private void FinishTurnRpc(RpcParams rpcParams = default)
        {
            if (State != GameState.Playing) return;
            if (ActivePlayer != rpcParams.Receive.SenderClientId) return;
            if (!DiceThrown) return;
            NextTurn();
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

        public void PlayDevelopmentCard(DevelopmentCard.Type cardType)
        {
            PlayDevelopmentCardRpc(cardType);
        }

        public void StealResource(ulong playerId)
        {
            if (CardLimitActive) return;
            StealResourceCardRpc(playerId);
        }

        [Rpc(SendTo.Authority)]
        private void StealResourceCardRpc(ulong playerId, RpcParams rpcparams = default)
        {
            if (!CanStealResource) return;
            if (ActivePlayer != rpcparams.Receive.SenderClientId) return;
            if (!PlayersInBanditRange().Contains(playerId)) return;

            var player = Player.GetPlayerById(playerId);
            var resourceToSteal = new Random().Next(1, player.ResourceCount);
            var count = 0;
            foreach (var tile in (Tile[])Enum.GetValues(typeof(Tile)))
            {
                count += player.GetResources(tile);
                if (resourceToSteal <= count)
                {
                    player.RemoveResources(tile, 1);
                    Player.GetPlayerById(ActivePlayer).AddResources(tile, 1);

                    _repositionBanditState.Value = 0;
                    ResourceCardsStolenRpc(ActivePlayer, tile, 1, RpcTarget.Single(playerId, RpcTargetUse.Temp));
                    return;
                }
            }
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void ResourceCardsStolenRpc(ulong playerId, Tile resource, byte amount, RpcParams rpcparams)
        {
            MessageHub.ResourcesStolen(playerId, resource, amount);
        }

        [Rpc(SendTo.Authority)]
        private void PlayDevelopmentCardRpc(DevelopmentCard.Type cardType, RpcParams rpcparams = default)
        {
            var senderId = rpcparams.Receive.SenderClientId;
            var player = Player.GetPlayerById(senderId);
            if (!player.HasDevelopmentCard(cardType)) return;

            switch (cardType)
            {
                case DevelopmentCard.Type.Knight:
                    player.KnightCardPlayed();
                    _repositionBanditState.Value.SetBitNoRef(0, true);
                    break;
                case DevelopmentCard.Type.HangedKnights:
                    foreach (var clientId in NetworkManager.ConnectedClientsIds)
                    {
                        if (clientId == senderId) continue;
                        Player.GetPlayerById(clientId).LimitKnightCards(1);
                    }
                    break;
                case DevelopmentCard.Type.VictoryPoint:
                    player.AddVictoryPoints(1);
                    break;
                case DevelopmentCard.Type.RoadBuilding:
                    player.AddFreeBuilding(BuildManager.BuildType.Street, 2);
                    break;
            }

            player.RemoveDevelopmentCard(cardType);
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

            GrantFreeBuildings();
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
                ShowGameOverClientRpc(ActivePlayer);
            }

            _hasThrownDice.Value = false;
            _playerTrades.Clear();
            _playerTurn.Value = (byte)((_playerTurn.Value + 1) % PlayerCount);
            if (_playerTurn.Value == 0)
                _roundNumber.Value += 1;

            if (PlayerCount == 1)
                PlayerTurnChange();
        }

        private void GrantFreeBuildings()
        {
            var player = Player.GetPlayerById(ActivePlayer);
            player.AddFreeBuilding(BuildManager.BuildType.Settlement);
            player.AddFreeBuilding(BuildManager.BuildType.Street);
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
            PlayerCardList.RollDice(ActivePlayer);
        }

        private void HandleFreeBuildingSelection()
        {
            if (!IsMyTurn()) return;
            if (!Player.LocalPlayer.HasFreeBuildings()) return;
            BuildManager.SelectBuildingType(Player.LocalPlayer.AvailableBuildings()[0]);
        }

        private void HasThrownDiceChange(bool previous, bool current)
        {
            DiceController.Instance.Reset();
        }

        private void TradeUpdated(TradeInfo info)
        {
            AvailableTradesMenu.UpdateAvailableTrades();
            if (info.ReceiverId == NetworkManager.LocalClientId)
            {
                MessageHub.TradeReceived(info);
            }
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
                _repositionBanditState.Value = _repositionBanditState.Value.SetBitNoRef(0, true);
                return;
            }
            foreach (var settlement in Settlement.AllSettlements)
            {
                if (!settlement.IsOccupied) continue;
                foreach (var tile in settlement.FindNeighboringTiles())
                {
                    if (!tile.Blocked && tile.Number == number)
                    {
                        Player.GetPlayerById(settlement.Owner).AddResources(tile.TileType, settlement.Level);
                    }
                }
            }
        }

        private void PlayerIdsChange(NetworkListEvent<ulong> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<ulong>.EventType.Add)
            {
                PlayerCardList.AddPlayerCard(Player.GetPlayerById(changeEvent.Value));
            } else if (changeEvent.Type == NetworkListEvent<ulong>.EventType.Remove)
            {
                PlayerCardList.RemovePlayerCard(changeEvent.Value);
            }
        }

        private void GameStateChange()
        {
            BuildManager.SetActive(false);
            DiceController.Instance.Reset();
            DevelopmentCardsDisplay.Open();
        }

        private void PlayerTurnChange()
        {
            TradeWindow.Close();
            DiceController.Instance.Reset();
            PlayerCardList.RollDice(ActivePlayer);
            TurnChanged?.Invoke();
            foreach (var clientId in _playerIds)
            {
                Player.GetPlayerById(clientId).ConvertBoughtCardsToAvailableOnes();
            }
        }

        private void RepositionBanditChange(byte previousValue, byte newValue)
        {
            if (IsMyTurn())
                CameraController.Instance.EnterOverview();
            if (RepositionBandit)
            {
                BuildManager.ShowInfoText("Bandit");
            }
            else if (CanStealResource)
            {
                BuildManager.ShowInfoText("Stealing Resource");
            }
            else if (newValue == 0)
            {
                BuildManager.SetActive(false);
            }
        }

        private void OnClientConnectionStatusChange(ulong clientId,
            ConnectionNotificationManager.ConnectionStatus connectionStatus)
        {
            if (NetworkManager.Singleton.IsHost)
            {
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
                        _cardsToDiscard.Clear();
                        _repositionBanditState.Value = 0;
                        NextTurn();
                        //  fix required: when player leaves, there is no check in place for who is supposed to be the next player etc.
                        _cardsToDiscard.RemoveAt(_playerIds.IndexOf(clientId));
                        _playerIds.Remove(clientId);
                        break;
                }
            }
        }

        private void OnClientStopped(bool isHost)
        {
            _ = LoadingScreen.PerformTasksInOrder(
                () => SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(0)),
                SceneManager.LoadSceneAsync(0, LoadSceneMode.Additive),
                SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene()));
            NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
            enabled = false;
            Camera.main.gameObject.SetActive(false);
        }
    }
}