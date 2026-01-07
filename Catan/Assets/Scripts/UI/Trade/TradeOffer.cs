using System;
using System.Collections.Generic;
using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using User;

namespace UI.Trade
{
    public class TradeOffer : MonoBehaviour
    {
        [SerializeField] private GameObject resourceIconPrefab;
        [SerializeField] private TextMeshProUGUI senderName;
        [SerializeField] private Transform receiveItemsParent;
        [SerializeField] private Transform payItemsParent;
        [SerializeField] private Button acceptTradeButton;

        private int _tradeId;

        private void Awake()
        {
            acceptTradeButton.onClick.AddListener(AcceptTrade);
        }

        public void Initialize(int id, TradeInfo trade)
        {
            _tradeId = id;
            senderName.text = Player.GetPlayerById(trade.SenderId).PlayerName;
            AddResourceIcons(trade.ReceiveResources, payItemsParent);
            AddResourceIcons(trade.SendResources, receiveItemsParent);
        }

        private void AddResourceIcons(IEnumerable<BuildManager.ResourceCosts> resources, Transform parent)
        {
            foreach (var resource in resources)
            {
                var icon = Instantiate(resourceIconPrefab).GetComponent<ResourceIcon>();
                icon.SetData(resource.resource, resource.amount);
                icon.transform.SetParent(parent);
            }
        }

        private void AcceptTrade()
        {
            acceptTradeButton.interactable = false;
            GameManager.Instance.AcceptTrade(_tradeId);
        }
    }
}