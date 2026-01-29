using System;
using GamePlay;
using UI.DevelopmentCards;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class TradeMenu : MonoBehaviour
    {
        public static TradeMenu Instance;

        public bool IsOpen => _open && CanBeOpened();
        public event Action OnOpen;

        [SerializeField] private Button closeButton;
        [SerializeField] private float animationSpeed;

        private bool _open;
        private RectTransform _rectTransform;
        private Vector2 _closedPosition;
        private Vector2 _openPosition;
        
        private void Awake()
        {
            Instance = this;
            _rectTransform = GetComponent<RectTransform>();
            _openPosition = _rectTransform.anchoredPosition;
            _closedPosition = _rectTransform.anchoredPosition - new Vector2(_rectTransform.sizeDelta.x, 0);
            _rectTransform.anchoredPosition = _closedPosition;
            closeButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            _rectTransform.anchoredPosition = Vector3.Lerp(_rectTransform.anchoredPosition, GetTargetPosition(), Time.deltaTime * animationSpeed);
            if (_open && !CanBeOpened()) Close();
        }

        public void Open()
        {
            if (GameManager.Instance.DiceThrown)
            {
                _open = true;
                OnOpen?.Invoke();
            }
        }

        private void Close()
        {
            _open = false;
        }

        private bool CanBeOpened()
        {
            if (!GameManager.Instance.DiceThrown) return false;
            if (DevelopmentCardsDisplay.HasToRevealCard) return false;
            if (GameManager.Instance.ActivePlayer != NetworkManager.Singleton.LocalClientId) return false;
            if (GameManager.Instance.RepositionBandit) return false;
            return true;
        }

        private Vector2 GetTargetPosition()
        {
            return IsOpen ? _openPosition : _closedPosition;
        }
    }
}
