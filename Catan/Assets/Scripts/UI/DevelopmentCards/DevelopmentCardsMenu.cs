using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using User;

namespace UI.DevelopmentCards
{
    [RequireComponent(typeof(CanvasGroup))]
    public class DevelopmentCardsMenu : MonoBehaviour
    {
        [Serializable]
        private struct CardDescription
        {
            public DevelopmentCard.Type card;
            [TextArea]
            public string description;
        }

        public static bool IsOpen { get; private set; }
        private static DevelopmentCardsMenu _instance;

        [SerializeField] private float closedOffset;
        [SerializeField] private float moveTime;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform signRect;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private float cardScale;
        [SerializeField] private Transform boughtCardsListTransform;
        [SerializeField] private Transform availableCardsListTransform;
        [SerializeField] private Tooltip cardTooltip;
        [SerializeField] private CardDescription[] cardDescriptions;

        private CanvasGroup _canvasGroup;
        private Vector2 defaultPosition;
        private readonly List<DevelopmentCard> _boughtCards = new();
        private readonly List<DevelopmentCard> _availableCards = new();

        private void Awake()
        {
            _instance = this;
            _canvasGroup = GetComponent<CanvasGroup>();
            closeButton.onClick.AddListener(() => StartCoroutine(Close()));
            defaultPosition = signRect.anchoredPosition;
            signRect.anchoredPosition = defaultPosition + Vector2.up * closedOffset;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            if (IsOpen)
                CheckForCardHover();
        }

        private void Start()
        {
            Player.LocalPlayer.DevelopmentCardBought += CardBought;
            GameManager.Instance.TurnChanged += TurnChanged;
        }

        public static void Open()
        {
            IsOpen = true;
            _instance.StartCoroutine(_instance.AnimatePosition(_instance.defaultPosition, _instance.moveTime));
            _instance._canvasGroup.interactable = true;
            _instance._canvasGroup.blocksRaycasts = true;
        }

        private IEnumerator Close()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            yield return AnimatePosition(defaultPosition + Vector2.up * closedOffset, moveTime);
            DevelopmentCardsDisplay.Open();
            IsOpen = false;
        }

        private IEnumerator AnimatePosition(Vector2 target, float time)
        {
            var t = 0f;
            var startPos = signRect.anchoredPosition;
            while (t < time)
            {
                t += Time.deltaTime;
                signRect.anchoredPosition = Vector2.Lerp(startPos, target, t / time);
                yield return null;
            }
            signRect.anchoredPosition = target;
        }

        private string GetDescription(DevelopmentCard.Type cardType)
        {
            return cardDescriptions.FirstOrDefault(card => card.card == cardType).description;
        }

        private void CardBought(DevelopmentCard.Type type)
        {
            var card = Instantiate(cardPrefab, boughtCardsListTransform).GetComponent<DevelopmentCard>();
            card.SetType(type, true);
            card.Scale = cardScale;
            _boughtCards.Add(card);
        }

        private void CheckForCardHover()
        {
            foreach (var card in _availableCards)
            {
                if (UpdateCardTooltip(card)) return;
            }
            foreach (var card in _boughtCards)
            {
                if (UpdateCardTooltip(card)) return;
            }
            cardTooltip.gameObject.SetActive(false);
        }

        private bool UpdateCardTooltip(DevelopmentCard card)
        {
            if (card.IsHovered)
            {
                cardTooltip.SetTooltip(card.Rect, GetDescription(card.CardType));
                return true;
            }
            return false;
        }

        private void TurnChanged()
        {
            _availableCards.AddRange(_boughtCards);
            foreach (var card in _boughtCards)
            {
                card.transform.SetParent(availableCardsListTransform);
                card.CardClicked += PlayCard;
            }
            _boughtCards.Clear();
        }

        private void PlayCard(DevelopmentCard card)
        {
            Debug.Log("Playing card: " + card.CardType);
        }
    }
}