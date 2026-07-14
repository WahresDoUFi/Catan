using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class TradeWindow : MonoBehaviour
    {
        public static bool IsOpen => _instance._isOpen;
        private static TradeWindow _instance;
        
        [SerializeField] private Button closeButton;
        [SerializeField] private Button menuSelectionButton;
        [SerializeField] private MenuSelectionDropdown menuSelectionDropdown;
        [Header("Animation")]
        [SerializeField] private float fadeTime;
        [SerializeField] private float animationScale;
        [SerializeField] private MonoBehaviour content;

        private bool _isOpen;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _instance = this;
            closeButton.onClick.AddListener(Close);
            menuSelectionButton.onClick.AddListener(menuSelectionDropdown.Open);
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.interactable = _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
        }

        public static void Open()
        {
            OpenWithMenu(_instance.menuSelectionDropdown.LastSelectedMenu);
        }

        public static void OpenWithMenu(int menuId)
        {
            _instance.menuSelectionDropdown.SelectMenu(menuId);
            if (IsOpen) return;
            _instance._isOpen = true;
            UITween.AnimateAlpha(_instance._canvasGroup, 0f, 1f, _instance.fadeTime);
            UITween.AnimateScale(_instance.content, _instance.animationScale, 1f, _instance.fadeTime);
            UITween.DelayAction(_instance, _instance.fadeTime, () => _instance._canvasGroup.interactable = _instance._canvasGroup.blocksRaycasts = true);
        }

        public static void Close()
        {
            if (!IsOpen) return;
            _instance._isOpen = false;
            _instance._canvasGroup.interactable = _instance._canvasGroup.blocksRaycasts = false;
            UITween.AnimateAlpha(_instance._canvasGroup, 1f, 0f, _instance.fadeTime);
            UITween.AnimateScale(_instance.content, 1f, _instance.animationScale, _instance.fadeTime);
        }
    }
}
