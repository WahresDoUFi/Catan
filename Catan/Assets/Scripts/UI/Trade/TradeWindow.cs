using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class TradeWindow : MonoBehaviour
    {
        public static bool IsOpen => _instance.gameObject.activeSelf;
        private static TradeWindow _instance;
        
        [SerializeField] private Button closeButton;
        [SerializeField] private Button menuSelectionButton;
        [SerializeField] private MenuSelectionDropdown menuSelectionDropdown;

        private void Awake()
        {
            _instance = this;
            closeButton.onClick.AddListener(Close);
            menuSelectionButton.onClick.AddListener(menuSelectionDropdown.Open);
            Close();
        }

        public static void Open()
        {
            _instance.gameObject.SetActive(true);
        }

        public static void Close()
        {
            _instance.gameObject.SetActive(false);
        }
    }
}
