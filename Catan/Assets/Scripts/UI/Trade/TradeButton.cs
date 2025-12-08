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
            _rectTransform.anchoredPosition = Vector3.Lerp(_rectTransform.anchoredPosition, GetTargetPosition(),
                animationSpeed * Time.deltaTime);
        }

        private void TradeButtonClicked()
        {
            if (GameManager.Instance.IsMyTurn())
                TradeMenu.Instance.Open();
            else
                TradeWindow.Open();
        }

        private Vector3 GetTargetPosition()
        {
            if (GameManager.Instance.State != GameManager.GameState.Playing) return _closedPosition;
            if (!GameManager.Instance.DiceThrown) return _closedPosition;
            return _defaultPosition;
        }
    }
}
