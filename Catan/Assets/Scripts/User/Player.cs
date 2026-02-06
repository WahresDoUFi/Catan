using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using UI;
using UI.DevelopmentCards;
using Unity.Netcode;
using UnityEngine;

namespace User
{
    public class Player : NetworkBehaviour
    {
        public static readonly List<Player> AllPlayers = new();
        public static Player LocalPlayer { get; private set; }
        public event Action ResourcesUpdated;
        public event Action<DevelopmentCard.Type> DevelopmentCardBought;
        public event Action<DevelopmentCard.Type> DevelopmentCardPlayed;

        public ulong PlayerId => OwnerClientId;
        public int ResourceCount => _wood.Value + _stone.Value + _wheat.Value + _brick.Value + _sheep.Value;
        public byte Wood => _wood.Value;
        public byte Stone => _stone.Value;
        public byte Wheat => _wheat.Value;
        public byte Brick => _brick.Value;
        public byte Sheep => _sheep.Value;
        public string PlayerName => _playerName;
        public int PictureId => _pictureId;
        public byte KnightCardsPlayed => _knightCards.Value;
        public byte AdditionalVictoryPoints => _victoryPoints.Value;
        public int VictoryPoints =>
            global::VictoryPoints.CalculateVictoryPoints(OwnerClientId);

        private readonly NetworkVariable<byte> _wood = new();
        private readonly NetworkVariable<byte> _stone = new();
        private readonly NetworkVariable<byte> _wheat = new();
        private readonly NetworkVariable<byte> _brick = new();
        private readonly NetworkVariable<byte> _sheep = new();
        private readonly NetworkVariable<byte> _victoryPoints = new();
        private readonly NetworkList<byte> _developmentCards = new();
        private readonly NetworkVariable<byte> _knightCards = new();
        private readonly NetworkList<byte> _freeBuildings = new();

        private readonly List<DevelopmentCard.Type> _boughtCards = new(); 
        private string _playerName;
        private int _pictureId;

        private void Awake()
        {
            AllPlayers.Add(this);
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                SetNameRpc(PlayerPrefs.GetString("Nickname"));
                SetPictureIdRpc(PlayerPrefs.GetInt("Character"));
                LocalPlayer = this;
                _knightCards.OnValueChanged += KnightCardsChanged;
            }
            else if (!IsHost)
            {
                RequestNameRpc();
                RequestPictureIdRpc();
            }

            _wood.OnValueChanged += ResourceCountChanged;
            _stone.OnValueChanged += ResourceCountChanged;
            _wheat.OnValueChanged += ResourceCountChanged;
            _brick.OnValueChanged += ResourceCountChanged;
            _sheep.OnValueChanged += ResourceCountChanged;
            _developmentCards.OnListChanged += DevelopmentCardsChanged;
        }

        public override void OnNetworkDespawn()
        {
            AllPlayers.Remove(this);
        }

        public static Player GetPlayerById(ulong clientId)
        {
            if (AllPlayers.Count == 0)
                Debug.LogWarning("Trying to get players before list is initialized");
            return AllPlayers.FirstOrDefault(player => player.OwnerClientId == clientId);
        }

        public void AddVictoryPoints(byte points)
        {
            _victoryPoints.Value += points;
        }

        public void KnightCardPlayed()
        {
            if (NetworkManager.IsHost == false) return;
            _knightCards.Value++;
        }

        public void LimitKnightCards(byte limit)
        {
            _knightCards.Value = (byte)Mathf.Min(_knightCards.Value, limit);
        }

        public bool HasDevelopmentCard(DevelopmentCard.Type cardType)
        {
            foreach (var card in _developmentCards)
            {
                if (card == (byte)cardType) return true;
            }
            return false;
        }

        public void RemoveDevelopmentCard(DevelopmentCard.Type card)
        {
            _developmentCards.Remove((byte)card);
        }

        public void BuyDevelopmentCard(DevelopmentCard.Type cardType)
        {
            if (IsServer)
            {
                DevelopmentCardBoughtRpc((byte)cardType);
            }
        }

        public void ConvertBoughtCardsToAvailableOnes()
        {
            if (NetworkManager.IsHost)
            {
                foreach (var card in _boughtCards)
                {
                    _developmentCards.Add((byte)card);
                }
            }
            _boughtCards.Clear();
        }

        public bool CanAfford(BuildManager.BuildType building)
        {
            if (_freeBuildings.Contains((byte)building)) return true;
            else return HasResources(BuildManager.GetCostsForBuilding(building));
        }

        public void Purchase(BuildManager.BuildType building)
        {
            if (_freeBuildings.Contains((byte)building))
            {
                _freeBuildings.Remove((byte)building);
                return;
            }
            var costs = BuildManager.GetCostsForBuilding(building);
            if (!HasResources(costs)) return;
            foreach (var cost in costs)
            {
                RemoveResources(cost.resource, cost.amount);
            }
        }

        public bool HasResources(BuildManager.ResourceCosts[] costs)
        {
            return costs.All(cost => GetResources(cost.resource) >= cost.amount);
        }

        public byte GetResources(Tile type)
        {
            return type switch
            {
                Tile.Grass => _sheep.Value,
                Tile.Stone => _stone.Value,
                Tile.Forest => _wood.Value,
                Tile.Brick => _brick.Value,
                Tile.Field => _wheat.Value,
                _ => 0
            };
        }

        public void AddResources(Tile type, byte amount)
        {
            switch (type)
            {
                case Tile.Forest:
                    _wood.Value += amount;
                    break;
                case Tile.Stone:
                    _stone.Value += amount;
                    break;
                case Tile.Field:
                    _wheat.Value += amount;
                    break;
                case Tile.Brick:
                    _brick.Value += amount;
                    break;
                case Tile.Grass:
                    _sheep.Value += amount;
                    break;
                default:
                    return;
            }
        }

        public void RemoveResources(Tile type, byte amount)
        {
            switch (type)
            {
                case Tile.Forest:
                    _wood.Value -= amount;
                    break;
                case Tile.Stone:
                    _stone.Value -= amount;
                    break;
                case Tile.Field:
                    _wheat.Value -= amount;
                    break;
                case Tile.Brick:
                    _brick.Value -= amount;
                    break;
                case Tile.Grass:
                    _sheep.Value -= amount;
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Give player buildings for free, that they have to immediately place
        /// </summary>
        /// <param name="type">The building to place</param>
        /// <param name="amount">default 1, how many of that type</param>
        public void AddFreeBuilding(BuildManager.BuildType type, int amount = 1)
        {
            if (!NetworkManager.IsHost) return;
            for (var i = 0; i < amount; i++)
            {
                _freeBuildings.Add((byte)type);
            }
        }

        public bool HasFreeBuildings()
        {
            return _freeBuildings.Count > 0;
        }

        public BuildManager.BuildType[] AvailableBuildings()
        {
            var result = new BuildManager.BuildType[_freeBuildings.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = (BuildManager.BuildType)_freeBuildings[i];
            }
            return result;
        }

        public IEnumerable<Harbor> GetHarbors()
        {
            foreach (var settlement in Settlement.AllSettlements)
            {
                if (settlement.Owner == PlayerId)
                {
                    if (settlement.HasHarbor())
                        yield return settlement.GetHarbor();
                }
            }
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
        private void DevelopmentCardBoughtRpc(byte card)
        {
            var cardType = (DevelopmentCard.Type)card;
            _boughtCards.Add(cardType);
            DevelopmentCardBought?.Invoke(cardType);
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
        private void SetNameRpc(string playerName)
        {
            _playerName = playerName;
        }

        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
        private void SetPictureIdRpc(int pictureId)
        {
            _pictureId = Mathf.Max(0, pictureId);
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void SendNameRpc(string playerName, RpcParams rpcparams)
        {
            _playerName = playerName;
        }

        [Rpc(SendTo.SpecifiedInParams, InvokePermission = RpcInvokePermission.Server)]
        private void SendPictureIdRpc(int pictureId, RpcParams rpcparams)
        {
            _pictureId = pictureId;
        }

        [Rpc(SendTo.Authority)]
        private void RequestNameRpc(RpcParams rpcparams = default)
        {
            SendNameRpc(_playerName, RpcTarget.Single(rpcparams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Authority)]
        private void RequestPictureIdRpc(RpcParams rpcparams = default)
        {
            SendPictureIdRpc(_pictureId, RpcTarget.Single(rpcparams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        private void ResourceCountChanged(byte previous, byte current)
        {
            ResourcesUpdated?.Invoke();
        }

        private void DevelopmentCardsChanged(NetworkListEvent<byte> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<byte>.EventType.Remove)
            {
                DevelopmentCardPlayed?.Invoke((DevelopmentCard.Type)changeEvent.Value);
            }
        }

        private void KnightCardsChanged(byte previousValue, byte newValue)
        {
            var change = previousValue - newValue;
            if (change > 0)
            {
                NotificationHub.KnightsHanged((byte)change);
            }
        }
    }
}