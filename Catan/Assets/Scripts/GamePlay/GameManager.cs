using System;
using System.Collections;
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
        public static int MaxCardsOnBandit => Instance == null ? 7 : Instance.maxCardsOnBandit.Value;
        private int VictoryPointsTarget => victoryPointsTarget.Value;
        private const float DisconnectWaitDelay = 10f;

        private const byte RepositionBanditBit = 0b1;
        private const byte StealResourcesBit = 0b10;
        private const byte MonopolyActiveBit = 0b100;
        private const byte YearOfPlentyActiveBit = 0b1000;

        //  only on host - used for reconnecting players
        private static readonly Dictionary<string, ulong> PlayerGuidsToClientId = new();
        
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
            GameOver,
        }

        public GameState State => (GameState)_gameState.Value;
        public float DisconnectWaitTime => _disconnectWaitTime.Value;
        public bool DiceThrown => _hasThrownDice.Value;
        public int Seed => _seed.Value;
        public ulong ActivePlayer => _playerIds[_playerTurn.Value];
        public bool IsGameOver => gameOverScreen.gameObject.activeSelf;
        public bool CardLimitActive => _cardsToDiscard.AsNativeArray().Any(cards => cards > 0);
        public int CardsToDiscard => _cardsToDiscard[LocalPlayerIndex];
        public bool SpecialActionActive => _specialActionState.Value != 0;
        public bool RepositionBandit => _specialActionState.Value == RepositionBanditBit;
        public bool CanStealResource => _specialActionState.Value == StealResourcesBit && !CardLimitActive;
        private int PlayerCount => _playerIds.Count;
        private int LocalPlayerIndex => Mathf.Max(0, _playerIds.IndexOf(NetworkManager.LocalClientId));
        public event Action TurnChanged;

        [SerializeField] private Color[] playerColors;
        [SerializeField] private GameOverScreen gameOverScreen;

        private readonly NetworkVariable<byte> _gameState = new();
        private readonly NetworkVariable<byte> _playerTurn = new();
        private readonly NetworkList<ulong> _playerIds = new();
        private readonly NetworkVariable<bool> _hasThrownDice = new();
        private readonly NetworkVariable<byte> _specialActionState = new();
        private readonly NetworkVariable<byte> _roundNumber = new();
        private readonly NetworkVariable<int> _seed = new();
        private readonly NetworkTradeInfoVariable _playerTrades = new();
        private readonly NetworkList<byte> _cardsToDiscard = new();
        private readonly NetworkVariable<float> _disconnectWaitTime = new();

        //  game settings
        public readonly NetworkVariable<int> victoryPointsTarget = new();
        public readonly NetworkVariable<int> maxCardsOnBandit = new();
        public readonly NetworkVariable<bool> revealTilesOnStart = new();

        private void Update()
        {
            if (!IsSpawned) return;
            HandleFreeBuildingSelection();
            if (!NetworkManager.IsHost) return;
            HandlePlayerDisconnectWaiting();
            if (State == GameState.Preparing)
            {
                if (Player.GetPlayerById(ActivePlayer)?.HasFreeBuildings() == false)
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
            Instance = this;
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
                SetupHarbors();
            }
            
            ConnectionNotificationManager.Instance.OnClientConnectionNotification += OnClientConnectionStatusChange;
            NetworkManager.Singleton.OnClientStopped += OnClientStopped;
            _gameState.OnValueChanged += (_, _) => GameStateChange();
            if (State != GameState.Waiting)
            {
                GameStateChange();
            }
            _playerTurn.OnValueChanged += (_, _) => PlayerTurnChange();
            _hasThrownDice.OnValueChanged += HasThrownDiceChange;
            _playerTrades.TradeUpdated += TradeUpdated;
            _playerTrades.TradeCleared += AvailableTradesMenu.UpdateAvailableTrades;
            _specialActionState.OnValueChanged += SpecialActionStateChange;
            _playerIds.OnListChanged += PlayerIdsChange;
            
            StartCoroutine(LateNetworkSpawn());
        }

        private IEnumerator LateNetworkSpawn()
        {
            yield return null;
            Street.UpdateAll();
            Settlement.UpdateAll();
        }

        public override void OnNetworkDespawn()
        {
            ConnectionNotificationManager.Instance.OnClientConnectionNotification -= OnClientConnectionStatusChange;
        }

        public static bool PlayerConnected(ulong clientId)
        {
            if (!Instance || !Instance._playerIds.Contains(clientId)) return false;
            return Player.GetPlayerById(clientId)?.IsConnected == true;
        }

        public static void SetPlayerGuid(ulong clientId, string guid)
        {
            if (PlayerGuidsToClientId.TryAdd(guid, clientId)) return;
            
            if (Instance)
            {
                ulong oldClientId = PlayerGuidsToClientId[guid];
                Instance._playerIds.Set(Instance._playerIds.IndexOf(PlayerGuidsToClientId[guid]), clientId, true);
                Street.ReplaceClientId(oldClientId, clientId);
                Settlement.ReplaceClientId(oldClientId, clientId);
                PlayerGuidsToClientId[guid] = clientId;   
            }
        }

        public static void InitializeUserData(string guid)
        {
            PlayerGuidsToClientId.Clear();
            PlayerGuidsToClientId.Add(guid, NetworkManager.Singleton.LocalClientId);
        }

        public static string GetPlayerUserId(ulong clientId)
        {
            foreach (var (userId, networkId) in PlayerGuidsToClientId)
            {
                if (networkId == clientId) return userId;
            }

            return string.Empty;
        }

        public bool PlayerGuidExists(string guid)
        {
            return PlayerGuidsToClientId.ContainsKey(guid);
        }

        public int GetPlayerIndex(ulong clientId)
        {
            return _playerIds.IndexOf(clientId);
        }

        public bool IsMyTurn()
        {
            if (State == GameState.Waiting || !NetworkManager.Singleton)
                return false;
            
            return _playerIds.Contains(NetworkManager.LocalClientId) && LocalPlayerIndex == _playerTurn.Value;
        }

        public IEnumerable<ulong> GetConnectedPlayerIds()
        {
            foreach (ulong clientId in _playerIds)
            {
                if (PlayerConnected(clientId))
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
            if (!Player.LocalPlayer.CanAfford(BuildManager.BuildType.Settlement)) return false;
            if (!settlement.CanBeBuildBy(NetworkManager.Singleton.LocalClientId)) return false;
            BuySettlementRpc(settlement.Id);
            return true;
        }

        public bool UpgradeSettlement(Settlement settlement)
        {
            if (!settlement) return false;
            if (!Player.LocalPlayer.CanAfford(BuildManager.BuildType.City)) return false;
            if (settlement.Level != 1) return false;
            UpgradeSettlementRpc(settlement.Id);
            return true;
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

        public bool CanBanditMoveTo(MapTile tile)
        {
            if (tile.Discovered == false) return false;
            if (tile.Blocked) return false;
            return true;
        }

        public void SetBanditTile(MapTile tile)
        {
            if (CanBanditMoveTo(tile))
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
            if (tile.Blocked) return;
            Bandit.Instance.SetTargetTile(tile);
            _specialActionState.Value = 0;
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

        public void SelectYearOfPlentyResources(BuildManager.ResourceCosts[] resources)
        {
            SelectYearOfPlentyResourcesRpc(resources);
        }

        [Rpc(SendTo.Authority)]
        private void SelectYearOfPlentyResourcesRpc(BuildManager.ResourceCosts[] resources,
            RpcParams rpcparams = default)
        {
            if (_specialActionState.Value != YearOfPlentyActiveBit) return;
            var player = Player.GetPlayerById(rpcparams.Receive.SenderClientId);
            if (player.PlayerId != ActivePlayer) return;
            if (resources.Sum(resource => resource.amount) != 2) return;

            foreach (var resource in resources)
            {
                player.AddResources(resource.resource, resource.amount);
            }

            _specialActionState.Value = 0;
        }

        private void CheckResourceStealAbilit()
        {
            if (!NetworkManager.IsHost) return;

            if (PlayersInBanditRange().Any())
                _specialActionState.Value = StealResourcesBit;
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
        private void BuySettlementRpc(int settlementId, RpcParams rpcparams = default)
        {
            var clientId = rpcparams.Receive.SenderClientId;
            if (clientId != ActivePlayer) return;
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
        private void UpgradeSettlementRpc(int settlementId, RpcParams rpcparams = default)
        {
            var clientId = rpcparams.Receive.SenderClientId;
            if (clientId != ActivePlayer) return;
            var settlement = Settlement.AllSettlements[settlementId];
            if (settlement.Owner != clientId && settlement.Level != 1) return;
            var player = Player.GetPlayerById(clientId);
            if (!player.CanAfford(BuildManager.BuildType.City)) return;
            player.Purchase(BuildManager.BuildType.City);
            settlement.Upgrade();
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
            if (give == get) return;
            TradeResourcesRpc((int)give, (int)get);
        }

        [Rpc(SendTo.Authority)]
        private void TradeResourcesRpc(int give, int get, RpcParams rpcparams = default)
        {
            if (give == get) return;
            var clientId = rpcparams.Receive.SenderClientId;
            if (clientId != ActivePlayer) return;
            var player = Player.GetPlayerById(clientId);
            byte tradeAmount = player.GetHarbors().Any(harbor => !harbor.IsResourceTrade) ? (byte)3 : (byte)4;
            TradeResources(player, (Tile)give, tradeAmount, (Tile)get);
        }

        public void PerformHarborTrade(Tile give, Tile get)
        {
            if (give == get) return;
            PerformHarborTradeRpc(give, get);
        }

        [Rpc(SendTo.Authority)]
        private void PerformHarborTradeRpc(Tile give, Tile get, RpcParams rpcparams = default)
        {
            if (give == get) return;
            var clientId = rpcparams.Receive.SenderClientId;
            if (clientId != ActivePlayer) return;
            var player = Player.GetPlayerById(clientId);
            foreach (var harbor in player.GetHarbors())
            {
                if (harbor.IsResourceTrade && harbor.Resource == give)
                {
                    TradeResources(player, give, 2, get);
                    return;
                }
            }
        }

        private void TradeResources(Player player, Tile give, byte giveAmount, Tile get)
        {
            var costs = new BuildManager.ResourceCosts[]
                { new BuildManager.ResourceCosts() { amount = giveAmount, resource = give } };
            if (!player.HasResources(costs)) return;
            player.RemoveResources(give, giveAmount);
            player.AddResources(get, 1);
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
            var resource = player.GetRandomResource();
            player.RemoveResources(resource, 1);
            Player.GetPlayerById(ActivePlayer).AddResources(resource, 1);
            ResourceCardsStolenRpc(ActivePlayer, resource, 1, RpcTarget.Single(playerId, RpcTargetUse.Temp));
            _specialActionState.Value = 0;
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void InformMonopolyDeclaredRpc(Tile resource, RpcParams rpcparams)
        {
            NotificationHub.MonopolyDeclared(resource);
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void ResourceCardsStolenRpc(ulong playerId, Tile resource, byte amount, RpcParams rpcparams)
        {
            NotificationHub.ResourcesStolen(playerId, resource, amount);
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
                    _specialActionState.Value = RepositionBanditBit;
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
                case DevelopmentCard.Type.Monopoly:
                    if (Player.AllPlayers.Any(otherPlayer => otherPlayer.PlayerId != senderId && otherPlayer.ResourceCount > 0))
                    {
                        _specialActionState.Value = MonopolyActiveBit;
                    }
                    break;
                case DevelopmentCard.Type.YearOfPlenty:
                    _specialActionState.Value = YearOfPlentyActiveBit;
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

        public void DeclareMonopoly(Tile resourceType)
        {
            DeclareMonopolyRpc(resourceType);
        }

        [Rpc(SendTo.Server)]
        private void DeclareMonopolyRpc(Tile resourceType, RpcParams rpcparams = default)
        {
            if (_specialActionState.Value != MonopolyActiveBit) return;
            var senderId = rpcparams.Receive.SenderClientId;
            if (senderId != ActivePlayer) return;

            byte resourceCount = 0;
            foreach (ulong clientId in _playerIds)
            {
                if (clientId == ActivePlayer) continue;
                InformMonopolyDeclaredRpc(resourceType, RpcTarget.Single(clientId, RpcTargetUse.Temp));
                var player = Player.GetPlayerById(clientId);
                byte resources = player.GetResources(resourceType);
                if (resources == 0)
                    continue;
                resourceCount += resources;
                player.RemoveResources(resourceType, resources);
                ResourceCardsStolenRpc(senderId, resourceType, resources, RpcTarget.Single(player.PlayerId, RpcTargetUse.Temp));
            }

            Player.GetPlayerById(senderId).AddResources(resourceType, resourceCount);
            _specialActionState.Value = 0;
        }

        public void StartGame()
        {
            _gameState.Value = (byte)GameState.Preparing;
            _roundNumber.Value = 1;

            if (revealTilesOnStart.Value)
            {
                foreach (var tile in MapGenerator.Instance.Tiles)
                {
                    tile.Discover();
                }
            }

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

        private void SetupHarbors()
        {
            var resources = ((Tile[])Enum.GetValues(typeof(Tile))).ToList();
            resources.Remove(Tile.Desert);
            foreach (var harbor in Harbor.AllHarbors)
            {
                if (harbor.IsResourceTrade)
                {
                    var resource = resources[new Random().Next(0, resources.Count)];
                    harbor.SetResource(resource);
                    resources.Remove(resource);
                }
            }
        }

        private void NextTurn()
        {
            int victoryPoints = VictoryPoints.CalculateVictoryPoints(ActivePlayer);
            if (victoryPoints >= VictoryPointsTarget)
            {
                _gameState.Value = (byte)GameState.GameOver;
                ShowGameOverClientRpc(ActivePlayer);
            }

            _hasThrownDice.Value = false;
            _playerTrades.Clear();
            _playerTurn.Value = (byte)((_playerTurn.Value + 1) % PlayerCount);
            if (!PlayerConnected(ActivePlayer))
            {
                NextTurn();
                return;
            }
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
            if (Player.LocalPlayer?.HasFreeBuildings() == true)
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
                NotificationHub.TradeReceived(info);
            }
        }

        private void GrantResources(int number)
        {
            if (number is 7)
            {
                for (var i = 0; i < _playerIds.Count; i++)
                {
                    int cardCount = Player.GetPlayerById(_playerIds[i]).ResourceCount;
                    if (cardCount > maxCardsOnBandit.Value)
                    {
                        _cardsToDiscard[i] = (byte)Mathf.FloorToInt(cardCount / 2f);
                    }
                }
                _specialActionState.Value = RepositionBanditBit;
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

        private void HandlePlayerDisconnectWaiting()
        {
            if (State != GameState.Playing) return;
            if (PlayerConnected(ActivePlayer))
            {
                _disconnectWaitTime.Value = DisconnectWaitDelay;
                return;
            }

            _disconnectWaitTime.Value -= Time.deltaTime;
            if (_disconnectWaitTime.Value > 0) return;

            _disconnectWaitTime.Value = DisconnectWaitDelay;
            byte cardsToRemove = _cardsToDiscard[_playerIds.IndexOf(ActivePlayer)];
            var player = Player.GetPlayerById(ActivePlayer);
            for (var i = 0; i < cardsToRemove; i++)
            {
                player.RemoveResources(player.GetRandomResource(), 1);
            }

            _cardsToDiscard[_playerIds.IndexOf(ActivePlayer)] = 0;
            if (!CardLimitActive)
                _specialActionState.Value = 0;
            NextTurn();
        }

        private void PlayerIdsChange(NetworkListEvent<ulong> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<ulong>.EventType.Add)
            {
                PlayerCardList.AddPlayerCard(Player.GetPlayerById(changeEvent.Value));
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

        private void SpecialActionStateChange(byte previousValue, byte newValue)
        {
            if (IsMyTurn())
                CameraController.Instance.EnterOverview();
            DevelopmentCardsMenu.Close();
            if (newValue == RepositionBanditBit)
            {
                BuildManager.ShowInfoText("Bandit");
            }
            else if (newValue == StealResourcesBit)
            {
                BuildManager.ShowInfoText("Stealing Resource");
            }
            else if (newValue == MonopolyActiveBit)
            {
                if (IsMyTurn())
                    MonopolySelection.Open();
                else
                    BuildManager.ShowInfoText("Monopoly");
            }
            else if (newValue == YearOfPlentyActiveBit)
            {
                if (IsMyTurn())
                    YearOfPlentySelection.Open();
            }
            else if (newValue == 0)
            {
                MonopolySelection.Close();
                YearOfPlentySelection.Close();
                BuildManager.SetActive(false);
            }
        }

        private void OnClientConnectionStatusChange(ulong clientId,
            ConnectionNotificationManager.ConnectionStatus connectionStatus)
        {
            PlayerCardList.RefreshPlayerCards();
            if (NetworkManager.Singleton.IsHost)
            {
                switch (connectionStatus)
                {
                    case ConnectionNotificationManager.ConnectionStatus.Connected:
                        {
                            _cardsToDiscard.Add(0);
                            if (State == GameState.Waiting)
                            {
                                _playerIds.Add(clientId);
                                if (NetworkManager.Singleton.ConnectedClientsIds.Count == 4)
                                {
                                    StartGame();
                                }
                            }
                            else
                            {
                                Player.GetPlayerByGuid(GetPlayerUserId(clientId)).NetworkObject.ChangeOwnership(clientId);
                            }

                            break;
                        }
                    case ConnectionNotificationManager.ConnectionStatus.Disconnected:
                        if (!_playerIds.Contains(clientId)) break;
                        if (State == GameState.Waiting)
                        {
                            var player = Player.GetPlayerById(clientId);
                            _playerIds.Remove(clientId);
                            PlayerGuidsToClientId.Remove(player.Guid);
                            player.NetworkObject.Despawn();
                        }
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