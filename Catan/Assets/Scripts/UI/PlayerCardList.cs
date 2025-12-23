using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using UnityEngine;
using User;

namespace UI
{
    public class PlayerCardList : MonoBehaviour
    {
        private static PlayerCardList _instance;
        
        [SerializeField] private GameObject playerCardPrefab;
        [SerializeField] private float animationSpeed;
        [SerializeField] private float spacing;
        [SerializeField] private float turnOffset;

        private readonly List<PlayerCard> _playerCards = new();

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            for (var i = 0; i < _playerCards.Count; i++)
            {
                var playerCard = _playerCards[i];
                playerCard.transform.localPosition = Vector3.Lerp(playerCard.transform.localPosition,
                    GetTargetPosition(i), Time.deltaTime * animationSpeed);
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
            return targetPosition;
        }
    }
}