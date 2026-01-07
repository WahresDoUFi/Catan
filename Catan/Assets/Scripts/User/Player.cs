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
        public static Player LocalPlayer { get; private set; }
        public event Action ResourcesUpdated;
        public event Action<DevelopmentCard.Type> DevelopmentCardBought;
        public int ResourceCount => _wood.Value + _stone.Value + _wheat.Value + _brick.Value + _sheep.Value;
        public byte Wood => _wood.Value;
        public byte Stone => _stone.Value;
        public byte Wheat => _wheat.Value;
        public byte Brick => _brick.Value;
        public byte Sheep => _sheep.Value;
        public string PlayerName => _playerName;

        public int VictoryPoints =>
            global::VictoryPoints.CalculateVictoryPoints(OwnerClientId);

        private readonly NetworkVariable<byte> _wood = new();
        private readonly NetworkVariable<byte> _stone = new();
        private readonly NetworkVariable<byte> _wheat = new();
        private readonly NetworkVariable<byte> _brick = new();
        private readonly NetworkVariable<byte> _sheep = new();
        private readonly NetworkVariable<byte> _victoryPoints = new();
        private readonly NetworkList<byte> _developmentCards = new();

        private readonly List<DevelopmentCard.Type> _boughtCards = new(); 
        private string _playerName;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                SetNameRpc(PlayerPrefs.GetString("Nickname"));
                LocalPlayer = this;   
            }

            _wood.OnValueChanged += ResourceCountChanged;
            _stone.OnValueChanged += ResourceCountChanged;
            _wheat.OnValueChanged += ResourceCountChanged;
            _brick.OnValueChanged += ResourceCountChanged;
            _sheep.OnValueChanged += ResourceCountChanged;
        }

        public static Player GetPlayerById(ulong clientId)
        {
            return NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
        }

        public void AddVictoryPoints(byte points)
        {
            _victoryPoints.Value += points;
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

        [Rpc(SendTo.Owner, InvokePermission = RpcInvokePermission.Server)]
        private void DevelopmentCardBoughtRpc(byte card)
        {
            var cardType = (DevelopmentCard.Type)card;
            _boughtCards.Add(cardType);
            DevelopmentCardBought?.Invoke(cardType);
        }

        [Rpc(SendTo.Authority, InvokePermission = RpcInvokePermission.Owner)]
        private void SetNameRpc(string playerName)
        {
            _playerName = playerName;
        }

        private void ResourceCountChanged(byte previous, byte current)
        {
            ResourcesUpdated?.Invoke();
        }
    }
}