using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using UI.Trade;
using UnityEngine;
using User;

namespace UI.DevelopmentCards
{
    public class DevelopmentCardsDisplay : MonoBehaviour, IHoverable
    {
        public static event Action CardRevealed;
        public static bool HasToRevealCard { get; private set; }

        private static DevelopmentCardsDisplay _instance;

        private bool IsHovered => _hovering && !_moving && DevelopmentCardsMenu.CanOpen();
        
        [SerializeField] private float hoverScale;
        [SerializeField] private float scaleSpeed;
        [SerializeField] private float moveTime;
        [SerializeField] private Vector3 closedOffset;
        [SerializeField] private float cardAdjustSpeed;
        [SerializeField] private float cardTargetScale;
        [SerializeField] private float cardSpacing;
        [SerializeField] private Transform cardParentTransform;
        [SerializeField] private Transform boughtCardsParent;
        [SerializeField] private Transform availableCardsParent;
        [SerializeField] private GameObject developmentCardPrefab;
        [SerializeField] private Transform centerPosition;
        [SerializeField] private Transform cardSpawnPosition;

        private bool _moving;
        private bool _hovering;
        private RectTransform _rectTransform;
        private Vector3 _defaultPosition;
        private readonly List<DevelopmentCard> _availableDevelopmentCards = new();
        private readonly List<DevelopmentCard> _boughtDevelopmentCards = new();
  
        private void Awake()
        {
            _instance = this;
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            _defaultPosition = _rectTransform.anchoredPosition;
            _rectTransform.anchoredPosition = _defaultPosition + closedOffset;
            Player.LocalPlayer.DevelopmentCardBought += DevelopmentCardBought;
            Player.LocalPlayer.DevelopmentCardPlayed += DevelopmentCardPlayed;
            GameManager.Instance.TurnChanged += TurnChanged;
        }

        private void TurnChanged()
        {
            _availableDevelopmentCards.AddRange(_boughtDevelopmentCards);
            foreach (var card in _boughtDevelopmentCards)
            {
                card.Scale = cardTargetScale;
            }
            _boughtDevelopmentCards.Clear();
        }

        private void Update()
        {
            float targetScale = IsHovered ? hoverScale : 1f;
            SetScale(Mathf.Lerp(GetScale(), targetScale, Time.deltaTime * scaleSpeed));
            UpdateBoughtCards();
            UpdateAvailableCards();
        }

        public static void Open()
        {
            _instance.StartCoroutine(_instance.AnimatePosition(_instance._defaultPosition, _instance.moveTime));
        }

        public IEnumerator Close()
        {
            yield return AnimatePosition(_defaultPosition + closedOffset, moveTime);
            DevelopmentCardsMenu.Open();
        }

        private IEnumerator AnimatePosition(Vector3 to, float duration)
        {
            var t = 0f;
            var startPos = _rectTransform.anchoredPosition;
            _moving = true;
            while (t < duration)
            {
                t += Time.deltaTime;
                _rectTransform.anchoredPosition = Vector3.Lerp(startPos, to, t / duration);
                yield return null;
            }
            _rectTransform.anchoredPosition = to;
            _moving = false;
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
            int cardCount = _boughtDevelopmentCards.Count(card => card.Revealed);
            for (var i = 0; i < _boughtDevelopmentCards.Count; i++)
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
            var targetPosition = parent.position + Vector3.right * (offset * cardSpacing);
            cardTransform.position =
                Vector3.Lerp(cardTransform.position, targetPosition, Time.deltaTime * cardAdjustSpeed);
        }

        private void DevelopmentCardBought(DevelopmentCard.Type type)
        {
            var card = Instantiate(developmentCardPrefab, cardParentTransform).GetComponent<DevelopmentCard>();
            card.transform.position = cardSpawnPosition.position;
            card.SetType(type);
            _boughtDevelopmentCards.Add(card);
            card.CardClicked += BoughtCardClicked;
            HasToRevealCard = true;
        }

        private void DevelopmentCardPlayed(DevelopmentCard.Type type)
        {
            var card = _availableDevelopmentCards.FirstOrDefault(c => c.CardType == type);
            if (card == null) return;
            _availableDevelopmentCards.Remove(card);
            Destroy(card.gameObject);
        }

        private void BoughtCardClicked(DevelopmentCard card)
        {
            if (Vector3.Distance(card.transform.position, centerPosition.position) < 20f)
            {
                card.RevealCard(CardRevealed);
                card.CardClicked -= BoughtCardClicked;
                HasToRevealCard = false;
            }
        }

        public void HoverUpdated(bool hovering)
        {
            _hovering = hovering;
        }

        public void Clicked()
        {
            if (IsHovered)
                StartCoroutine(Close());
        }
    }
}
