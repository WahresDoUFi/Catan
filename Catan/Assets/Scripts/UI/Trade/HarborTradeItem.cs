using GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class HarborTradeItem : MonoBehaviour
    {
        [SerializeField] private Image profileIcon;
        [SerializeField] private Image resourceIcon;
        [SerializeField] private Button tradeButton;
        [SerializeField] private TMP_Dropdown resourceDropdown;

        private Tile _resource;

        private void Start()
        {
            tradeButton.onClick.AddListener(PerformTrade);
        }

        private void Update()
        {
            tradeButton.interactable = (int)_resource != resourceDropdown.value;
        }

        public void SetHarbor(Harbor harbor)
        {
            _resource = harbor.Resource;
            profileIcon.sprite = harbor.TraderIcon;
            resourceIcon.sprite = ResourceDataProvider.GetIcon(harbor.Resource);
        }

        private void PerformTrade()
        {
            GameManager.Instance.PerformHarborTrade(_resource, (Tile)resourceDropdown.value);
        }
    }
}
