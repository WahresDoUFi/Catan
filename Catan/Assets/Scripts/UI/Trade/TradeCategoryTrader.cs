using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class TradeCategoryTrader : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown giveResourceDropdown;
        [SerializeField] private TMP_Dropdown getResourceDropdown;
        [SerializeField] private Button buyButton;

        private void Awake()
        {
            buyButton.onClick.AddListener(PerformTrade);
        }

        private void PerformTrade()
        {
            GameManager.Instance.TradeResources((Tile)giveResourceDropdown.value, (Tile)getResourceDropdown.value);
        }
    }
}
