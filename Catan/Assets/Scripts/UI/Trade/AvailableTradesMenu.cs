using System;
using UnityEngine;

namespace UI.Trade
{
    public class AvailableTradesMenu : MonoBehaviour
    {
        private static AvailableTradesMenu _instance;
        [SerializeField] private GameObject tradeOfferPrefab;
        [SerializeField] private Transform tradeOfferListParent;

        private void Awake()
        {
            _instance = this;
        }

        private void OnEnable()
        {
            UpdateTradeOfferList();
        }

        public static void UpdateAvailableTrades()
        {
            _instance.UpdateTradeOfferList();
        }

        private void UpdateTradeOfferList()
        {
            ClearList();
            PopulateTradeList();
        }

        private void ClearList()
        {
            for (var i = 0; i < tradeOfferListParent.childCount; i++)
            {
                Destroy(tradeOfferListParent.GetChild(i).gameObject);
            }
        }

        private void PopulateTradeList()
        {
            if (!GameManager.Instance) return;
            foreach (var trade in GameManager.Instance.GetAvailableTrades())
            {
                var tradeOffer = Instantiate(tradeOfferPrefab, tradeOfferListParent).GetComponent<TradeOffer>();
                tradeOffer.Initialize(GameManager.Instance.GetTradeId(trade), trade);
            }
        }
    }
}