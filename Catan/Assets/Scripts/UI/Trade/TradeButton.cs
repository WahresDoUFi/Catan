using System;
using GamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class TradeButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private float animationSpeed;
        [SerializeField] private Vector3 closedOffset;

        private RectTransform _rectTransform;
        private Vector3 _defaultPosition;
        private Vector2 _closedPosition;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _defaultPosition = _rectTransform.anchoredPosition;
            _closedPosition = _defaultPosition + closedOffset;
            _rectTransform.anchoredPosition = _closedPosition;
        }

        private void Start()
        {
            button.onClick.AddListener(TradeButtonClicked);
        }

        private void Update()
        {
            button.interactable = IsAvailable();
            _rectTransform.anchoredPosition = Vector3.Lerp(_rectTransform.anchoredPosition, GetTargetPosition(),
                animationSpeed * Time.deltaTime);
        }

        private void TradeButtonClicked()
        {
            if (GameManager.Instance.IsMyTurn())
                TradeMenu.Instance.Open();
            else
                TradeWindow.Open();
            BuildManager.SetActive(false);
        }

        private static bool IsAvailable()
        {
            if (GameManager.Instance.State != GameManager.GameState.Playing) return false;
            if (!GameManager.Instance.DiceThrown) return false;
            if (GameManager.Instance.CardLimitActive) return false;
            if (GameManager.Instance.SpecialActionActive) return false;
            return true;
        }

        private Vector3 GetTargetPosition()
        {
            return IsAvailable() ? _defaultPosition : _closedPosition;
        }
    }
}
