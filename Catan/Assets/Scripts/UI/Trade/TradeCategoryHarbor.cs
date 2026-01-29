using System;
using Unity.Netcode;
using UnityEngine;

namespace UI.Trade
{
    public class TradeCategoryHarbor : MonoBehaviour
    {
        [SerializeField] private GameObject harborTradeItemPrefab;
        [SerializeField] private Transform harborTradeView;

        private void Awake()
        {
            Settlement.OnSettlementBuild += OnSettlementBuild;
        }

        private void OnDestroy()
        {
            Settlement.OnSettlementBuild -= OnSettlementBuild;
        }

        private void OnSettlementBuild(Settlement settlement)
        {
            if (settlement.Owner != NetworkManager.Singleton.LocalClientId) return;
            var harbor = settlement.GetHarbor();
            if (harbor && harbor.IsResourceTrade)
            {
                var tradeItem = Instantiate(harborTradeItemPrefab, harborTradeView).GetComponent<HarborTradeItem>();
                tradeItem.SetHarbor(harbor);
            }
        }
    }
}
