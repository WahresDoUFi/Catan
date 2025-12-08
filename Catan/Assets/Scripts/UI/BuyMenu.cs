using GamePlay;
using UnityEngine;

namespace UI
{
    public class BuyMenu : MonoBehaviour
    {
        [SerializeField] private Vector3 closedOffset;
        [SerializeField] private float animationSpeed;
        
        private Vector3 _defaultPosition;
        private Vector3 _targetPosition;
        private RectTransform _rectTransform;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _defaultPosition = _rectTransform.anchoredPosition;
            _targetPosition = _defaultPosition + closedOffset;
            _rectTransform.anchoredPosition = _targetPosition;
        }

        private void Update()
        {
            _rectTransform.anchoredPosition = Vector3.Lerp(_rectTransform.anchoredPosition, 
                _targetPosition, Time.deltaTime * animationSpeed);
            UpdateTargetPosition();
        }

        private void UpdateTargetPosition()
        {
            if (GameManager.Instance.State == GameManager.GameState.Playing)
            {
                _targetPosition = GameManager.Instance.IsMyTurn() && GameManager.Instance.CanThrowDice() == false ? _defaultPosition : _defaultPosition + closedOffset;
            }
            else
            {
                _targetPosition = _defaultPosition + closedOffset;   
            }
        }
    }
}
