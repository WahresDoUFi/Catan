using System;
using System.Collections.Generic;
using GamePlay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using User;

namespace UI.Trade
{
    public class CreateTradeMenu : MonoBehaviour
    {
        [SerializeField] private GameObject tabButtonPrefab;
        [SerializeField] private Transform tabButtonsParent;

        [SerializeField] private ResourceCounter[] otherPlayerResources;
        [SerializeField] private ResourceCounter[] playerResources;

        [SerializeField] private Button createTradeButton;

        private ulong _targetId;

        private void Awake()
        {
            createTradeButton.onClick.AddListener(CreateTrade);
        }

        private void Start()
        {
            var localPlayer = Player.LocalPlayer;
            foreach (var resourceCounter in playerResources)
            {
                resourceCounter.SetPlayer(localPlayer);
            }
            UpdatePlayerList();
        }

        private void OnEnable()
        {
            if (!GameManager.Instance) return;

            UpdatePlayerList();
            foreach (var resourceCounter in playerResources)
            {
                resourceCounter.Reset();
            }

            foreach (var resourceCounter in otherPlayerResources)
            {
                resourceCounter.Reset();
            }
        }

        private void UpdatePlayerList()
        {
            for (var i = 0; i < tabButtonsParent.childCount; i++)
            {
                Destroy(tabButtonsParent.GetChild(i).gameObject);
            }

            TabButton firstButton = null;
            ulong firstClientId = 0;

            if (GameManager.Instance.IsMyTurn())
            {
                foreach (ulong clientId in GameManager.Instance.GetPlayerIds())
                {
                    if (NetworkManager.Singleton.LocalClientId == clientId) continue;
                    var tabButton = Instantiate(tabButtonPrefab, tabButtonsParent).GetComponent<TabButton>();
                    tabButton.SetText(Player.GetPlayerById(clientId).PlayerName);
                    ulong id = clientId;
                    tabButton.OnClick.AddListener(() => ButtonPressed(tabButton, id));
                    if (!firstButton)
                    {
                        firstButton = tabButton;
                        firstClientId = clientId;
                    }
                }
            }
            else
            {
                var tabButton = Instantiate(tabButtonPrefab, tabButtonsParent).GetComponent<TabButton>();
                tabButton.SetText(Player.GetPlayerById(GameManager.Instance.ActivePlayer).PlayerName);
                tabButton.OnClick.AddListener(() => ButtonPressed(tabButton, GameManager.Instance.ActivePlayer));
                firstButton = tabButton;
                firstClientId = GameManager.Instance.ActivePlayer;
            }
            
            ButtonPressed(firstButton, firstClientId);
        }

        private void ButtonPressed(TabButton button, ulong clientId)
        {
            if (!button) return;
            for (var i = 0; i < tabButtonsParent.childCount; i++)
            {
                tabButtonsParent.GetChild(i).GetComponent<Button>().interactable = true;
            }
            button.Interactable = false;
            _targetId = clientId;
            var player = Player.GetPlayerById(clientId);
            foreach (var resourceCounter in otherPlayerResources)
            {
                resourceCounter.SetPlayer(player);
            }
        }

        private void CreateTrade()
        {
            var tradeInfo = new TradeInfo
            {
                SenderId = NetworkManager.Singleton.LocalClientId,
                ReceiverId = _targetId,
                SendResources = GetSendResources(),
                ReceiveResources = GetReceiveResources()
            };
            GameManager.Instance.CreateTrade(tradeInfo);
        }

        private BuildManager.ResourceCosts[] GetSendResources()
        {
            return CreateResourceList(playerResources);
        }

        private BuildManager.ResourceCosts[] GetReceiveResources()
        {
            return CreateResourceList(otherPlayerResources);
        }
        
        private BuildManager.ResourceCosts[] CreateResourceList(IEnumerable<ResourceCounter> counters)
        {
            List<BuildManager.ResourceCosts> resources = new();
            foreach (var resource in counters)
            {
                if (resource.Value == 0) continue;
                resources.Add(new BuildManager.ResourceCosts()
                {
                    amount = resource.Value,
                    resource = resource.Resource
                });
            }
            return resources.ToArray();
        }
    }

    public struct TradeInfo : INetworkSerializable, IEquatable<TradeInfo>
    {
        public ulong SenderId;
        public ulong ReceiverId;
        public BuildManager.ResourceCosts[] SendResources;
        public BuildManager.ResourceCosts[] ReceiveResources;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SenderId);
            serializer.SerializeValue(ref ReceiverId);

            SerializeResourceCostsArray(serializer, ref SendResources);
            SerializeResourceCostsArray(serializer, ref ReceiveResources);
        }

        private static void SerializeResourceCostsArray<T>(
            BufferSerializer<T> serializer,
            ref BuildManager.ResourceCosts[] array
        ) where T : IReaderWriter
        {
            if (serializer.IsWriter)
            {
                int length = array != null ? array.Length : 0;
                serializer.SerializeValue(ref length);

                for (int i = 0; i < length; i++)
                {
                    var element = array[i];
                    serializer.SerializeValue(ref element);
                }
            }
            else
            {
                int length = 0;
                serializer.SerializeValue(ref length);

                array = new BuildManager.ResourceCosts[length];

                for (int i = 0; i < length; i++)
                {
                    var element = new BuildManager.ResourceCosts();
                    serializer.SerializeValue(ref element);
                    array[i] = element;
                }
            }
        }

        public bool Equals(TradeInfo other)
        {
            return SenderId == other.SenderId && ReceiverId == other.ReceiverId;
        }

        public override bool Equals(object obj)
        {
            return obj is TradeInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SenderId, ReceiverId, SendResources, ReceiveResources);
        }
    }

    public class NetworkTradeInfoVariable : NetworkVariableBase
    {
        private readonly List<TradeInfo> _previousBuffer = new();
        public readonly List<TradeInfo> Trades = new();
        public Action<TradeInfo> TradeUpdated;
        public Action TradeCleared;

        public void AddTrade(TradeInfo trade)
        {
            int index = Trades.IndexOf(trade);
            Trades.Add(trade);
            if (index != -1)
                Trades.RemoveAt(index);
            else
                TradeUpdated?.Invoke(trade);
            SetDirty(true);
        }

        public void RemoveTrade(TradeInfo trade)
        {
            Trades.Remove(trade);
            SetDirty(true);
            TradeCleared?.Invoke();
        }

        public void Clear()
        {
            Trades.Clear();
            SetDirty(true);
            TradeCleared?.Invoke();
        }
        
        public override void WriteField(FastBufferWriter writer)
        {
            writer.WriteValueSafe(Trades.Count);
            foreach (var trade in Trades)
            {
                writer.WriteValueSafe(trade.SenderId);
                writer.WriteValueSafe(trade.ReceiverId);
                writer.WriteValueSafe(trade.SendResources.Length);
                foreach (var resource in trade.SendResources)
                {
                    writer.WriteValueSafe((int)resource.resource);
                    writer.WriteValueSafe(resource.amount);
                }
                writer.WriteValueSafe(trade.ReceiveResources.Length);
                foreach (var resource in trade.ReceiveResources)
                {
                    writer.WriteValueSafe((int)resource.resource);
                    writer.WriteValueSafe(resource.amount);
                }
            }
        }

        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out int itemsToUpdate);
            Trades.Clear();
            for (var i = 0; i < itemsToUpdate; i++)
            {
                var trade = new TradeInfo();
                reader.ReadValueSafe(out trade.SenderId);
                reader.ReadValueSafe(out trade.ReceiverId);
                reader.ReadValueSafe(out int sendResourceCount);
                var sendResources = new BuildManager.ResourceCosts[sendResourceCount];
                for (var j = 0; j < sendResourceCount; j++)
                {
                    reader.ReadValueSafe(out int resource);
                    sendResources[j].resource = (Tile)resource;
                    reader.ReadValueSafe(out byte amount);
                    sendResources[j].amount = amount;
                }
                trade.SendResources = sendResources;

                reader.ReadValueSafe(out int receiveResourceCount);
                var receiveResources = new BuildManager.ResourceCosts[receiveResourceCount];
                for (var j = 0; j < receiveResourceCount; j++)
                {
                    reader.ReadValueSafe(out int resource);
                    receiveResources[j].resource = (Tile)resource;
                    reader.ReadValueSafe(out byte amount);
                    receiveResources[j].amount = amount;
                }
                trade.ReceiveResources = receiveResources;
                Trades.Add(trade);
                if (_previousBuffer.Contains(trade))
                    _previousBuffer.Remove(trade);
                else
                    TradeUpdated?.Invoke(trade);
            }
            if (Trades.Count == 0 || _previousBuffer.Count > 0)
                TradeCleared?.Invoke();
            _previousBuffer.Clear();
            _previousBuffer.AddRange(Trades);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            ReadField(reader);
        }
        
        public override void WriteDelta(FastBufferWriter writer)
        {
            WriteField(writer);
        }
    }
}
