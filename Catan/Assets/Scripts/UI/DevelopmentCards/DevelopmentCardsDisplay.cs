using System.Collections;
using System.Collections.Generic;
using GamePlay;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.DevelopmentCards
{
    public class DevelopmentCardsDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private bool IsHovered => _hovering && GameManager.Instance.IsMyTurn() && GameManager.Instance.DiceThrown;
        
        [SerializeField] private float hoverScale;
        [SerializeField] private float scaleSpeed;
        [SerializeField] private float moveTime;
        [SerializeField] private Vector3 closedOffset;
        [SerializeField] private float cardAdjustSpeed;
        [SerializeField] private float cardTargetScale;
        [SerializeField] private float cardSpacing;
        [SerializeField] private Transform boughtCardsParent;
        [SerializeField] private Transform availableCardsParent;
        [SerializeField] private GameObject developmentCardPrefab;
        [SerializeField] private Transform centerPosition;
        
        private bool _hovering;
        private RectTransform _rectTransform;
        private Vector3 _defaultPosition;
        private readonly List<DevelopmentCard> _availableDevelopmentCards = new();
        private readonly List<DevelopmentCard> _boughtDevelopmentCards = new();
  
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            _defaultPosition = _rectTransform.anchoredPosition;
        }

        private void Update()
        {
            float targetScale = IsHovered ? hoverScale : 1f;
            SetScale(Mathf.Lerp(GetScale(), targetScale, Time.deltaTime * scaleSpeed));
            UpdateBoughtCards();
            UpdateAvailableCards();
        }

        public void Open()
        {
            StartCoroutine(AnimatePosition(_defaultPosition, moveTime));
        }

        public void Close()
        {
            StartCoroutine(AnimatePosition(_defaultPosition + closedOffset, moveTime));
        }

        private IEnumerator AnimatePosition(Vector3 to, float duration)
        {
            var t = 0f;
            var startPos = _rectTransform.anchoredPosition;
            while (t < duration)
            {
                t += Time.deltaTime;
                _rectTransform.anchoredPosition = Vector3.Lerp(startPos, to, t / duration);
                yield return null;
            }
            _rectTransform.anchoredPosition = to;
        }

        private float GetScale()
        {
            return transform.localScale.x;
        }
        private void SetScale(float scale)
        {
            transform.localScale = Vector3.one * scale;
        }

        private void UpdateBoughtCards()
        {
            int cardCount = _boughtDevelopmentCards.Count;
            for (var i = 0; i < cardCount; i++)
            {
                var card = _boughtDevelopmentCards[i];
                if (card.Revealed)
                {
                    UpdateCardPosition(card.transform, i, cardCount, boughtCardsParent);
                    card.Scale = Mathf.Lerp(card.Scale, cardTargetScale, Time.deltaTime * cardAdjustSpeed);
                }
                else
                {
                    card.transform.position = Vector3.Lerp(card.transform.position, centerPosition.position,
                        cardAdjustSpeed * Time.deltaTime);
                    card.Scale = Mathf.Lerp(card.Scale, 1f, Time.deltaTime * cardAdjustSpeed);
                }
            }
        }

        private void UpdateAvailableCards()
        {
            int cardCount = _availableDevelopmentCards.Count;
            for (var i = 0; i < cardCount; i++)
            {
                UpdateCardPosition(_availableDevelopmentCards[i].transform, i, cardCount, availableCardsParent);
            }
        }

        private void UpdateCardPosition(Transform cardTransform, int index, int cardCount, Transform parent)
        {
            float offset = index - (cardCount / 2f) + 0.5f;
            var targetPosition = boughtCardsParent.position;
            cardTransform.position =
                Vector3.Lerp(cardTransform.position, targetPosition, Time.deltaTime * cardAdjustSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovering = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //  open menu
        }
    }
}
