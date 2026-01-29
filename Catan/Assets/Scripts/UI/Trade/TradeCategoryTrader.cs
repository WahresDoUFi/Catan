using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using User;
using System.Linq;

namespace UI.Trade
{
    public class TradeCategoryTrader : MonoBehaviour
    {
        private const string description = "Trade {0}:1 with the Trader";
        private const string youGive = "You give up (x{0}):";

        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI youGiveText;
        [SerializeField] private TMP_Dropdown giveResourceDropdown;
        [SerializeField] private TMP_Dropdown getResourceDropdown;
        [SerializeField] private Button buyButton;

        private void Awake()
        {
            buyButton.onClick.AddListener(PerformTrade);
        }

        private void Start()
        {
            TradeMenu.Instance.OnOpen += UpdateInfoText;
        }

        private void UpdateInfoText()
        {
            int tradeValue = Player.LocalPlayer.GetHarbors().Any(harbor => !harbor.IsResourceTrade) ? 3 : 4;
            descriptionText.text = string.Format(description, tradeValue);
            youGiveText.text = string.Format(youGive, tradeValue);
        }

        private void PerformTrade()
        {
            GameManager.Instance.TradeResources((Tile)giveResourceDropdown.value, (Tile)getResourceDropdown.value);
        }
    }
}
