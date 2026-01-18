using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using Unity.Netcode;
using UnityEngine;
using User;

namespace UI
{
    public class PlayerCardList : MonoBehaviour
    {
        private static PlayerCardList _instance;
        
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private Transform screenCenter;
        [SerializeField] private float animationSpeed;
        [SerializeField] private float spacing;
        [SerializeField] private float stealModeSpacing;
        [SerializeField] private float turnOffset;

        private readonly List<PlayerCard> _playerCards = new();

        private float _cardWidth;

        private void Awake()
        {
            _instance = this;
            _cardWidth = playerCardPrefab.GetComponent<RectTransform>().sizeDelta.x;
        }

        private void Update()
        {
            if (GameManager.Instance.CanStealResource && GameManager.Instance.IsMyTurn())
            {
                byte cardsDisplayed = 0;
                var localPlayerIndex = GameManager.Instance.GetPlayerIds().ToList().IndexOf(NetworkManager.Singleton.LocalClientId);
                var playersInRange = GameManager.Instance.PlayersInBanditRange().ToArray();
                for (var index = 0; index < _playerCards.Count; index++)
                {
                    var playerCard = _playerCards[index];
                    if (index != localPlayerIndex && playersInRange.Contains(playerCard.PlayerId))
                    {
                        playerCard.transform.localPosition = Vector3.Lerp(playerCard.transform.localPosition,
                            GetTargetPositionStealMode(cardsDisplayed), Time.deltaTime * animationSpeed);
                    }
                }
            } 
            else
            {
                for (var i = 0; i < _playerCards.Count; i++)
                {
                    var playerCard = _playerCards[i];
                    playerCard.transform.localPosition = Vector3.Lerp(playerCard.transform.localPosition,
                        GetTargetPosition(i), Time.deltaTime * animationSpeed);
                }
            }
        }

        public static void AddPlayerCard(Player player)
        {
            var card = Instantiate(_instance.playerCardPrefab, _instance.transform).GetComponent<PlayerCard>();
            card.SetPlayer(player);
            _instance._playerCards.Add(card);
        }

        public static void RollDice(ulong playerId)
        {
            foreach (var card in _instance._playerCards)
            {
                if (GameManager.Instance.State == GameManager.GameState.Playing &&
                    card.PlayerId == playerId)
                {
                    card.RollDice();
                }
                else
                {
                    card.HideDice();
                }
            }
        }

        public static void RemovePlayerCard(ulong clientId)
        {
            var card = _instance._playerCards.FirstOrDefault(card => card.PlayerId == clientId);
            if (card)
            {
                _instance._playerCards.Remove(card);
                Destroy(card.gameObject);
            }
        }

        private Vector3 GetTargetPosition(int index)
        {
            float cardCount = _playerCards.Count;
            float offset = (cardCount - index - 1) - (cardCount / 2f) + 0.5f;
            var targetPosition = Vector3.up * (offset * spacing);
            if (GameManager.Instance.State == GameManager.GameState.Playing &&
                _playerCards[index].PlayerId == GameManager.Instance.ActivePlayer)
            {
                targetPosition += Vector3.right * turnOffset;
            }
            targetPosition += Vector3.right * (_cardWidth / 2f);
            return targetPosition;
        }

        private Vector3 GetTargetPositionStealMode(int index)
        {
            float count = _playerCards.Count - 1;
            float offset = (count - index - 1) - (count / 2f) + 0.5f;
            var targetPosition = Vector3.right * (offset * stealModeSpacing);
            return transform.InverseTransformPoint(screenCenter.position + targetPosition);
        }
    }
}