using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace UI
{
    public class ResourceCardsHolder : MonoBehaviour
    {
        [Serializable]
        private struct ResourceIcon
        {
            public Player.ResourceType resourceType;
            public Sprite icon;
        }
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private ResourceIcon[] resourceSprites;
        [SerializeField] private float maxCardTilt;
        [SerializeField] private float cardSpacing;
        [SerializeField] private float cardMoveSpeed = 5f;
        
        private Player _player;
        
        private readonly List<ResourceCard> _resourceCards = new();
        private int _lastHoveredCardIndex;
        private void Update()
        {
            if (!NetworkManager.Singleton) return;
            if (!NetworkManager.Singleton.IsConnectedClient)
            {
                ClearCards();
                return;
            };
            UpdatePlayer();
            UpdateCardPositions();
        }

        private void ClearCards()
        {
            if (transform.childCount == 0) return;
            for (var i = 0; i < transform.childCount; i++)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
            _resourceCards.Clear();
        }

        private void UpdateCardPositions()
        {
            int cardCount = _resourceCards.Count;
            if (cardCount == 1)
            {
                _resourceCards[0].transform.position = Vector3.Lerp(_resourceCards[0].transform.position,
                    transform.position, Time.deltaTime * cardMoveSpeed);
                _resourceCards[0].transform.rotation = Quaternion.Lerp(_resourceCards[0].transform.rotation,
                    Quaternion.identity, Time.deltaTime * cardMoveSpeed);
                return;
            }

            var hoveredCard = _resourceCards.FirstOrDefault(card => card.Hover);
            if (hoveredCard)
            {
                _lastHoveredCardIndex = _resourceCards.IndexOf(hoveredCard);
            }
            
            for (var i = 0; i < cardCount; i++)
            {
                var card = _resourceCards[i];
                int distanceToHoveredCard = Mathf.Abs(_lastHoveredCardIndex - i) + 1;
                card.transform.SetSiblingIndex(transform.childCount - distanceToHoveredCard);
                float offset = i - (cardCount / 2f);
                if (cardCount % 2 != 0) offset += 0.5f;
                var targetPosition = Vector3.right * (offset * cardSpacing);
                float targetRotation = Mathf.Lerp(maxCardTilt, -maxCardTilt, (float)i / (float)cardCount);
                card.transform.localPosition = Vector3.Lerp(card.transform.localPosition,
                    targetPosition, Time.deltaTime * cardMoveSpeed);
                card.transform.rotation = Quaternion.Lerp(card.transform.rotation,
                    Quaternion.Euler(0f, 0f, targetRotation), Time.deltaTime * cardMoveSpeed);
            }
        }

        private void UpdateResourceCards()
        {
            UpdateCardCount();
            UpdateCardIcons();
        }

        private void UpdateCardIcons()
        {
            var index = 0;
            for (var i = 0; i < _player.Wood; i++)
            {
                _resourceCards[index].SetIcon(GetResourceIcon(Player.ResourceType.Wood));
                index++;
            }
            for (var i = 0; i < _player.Stone; i++)
            {
                _resourceCards[index].SetIcon(GetResourceIcon(Player.ResourceType.Stone));
                index++;
            }
            for (var i = 0; i < _player.Wheat; i++)
            {
                _resourceCards[index].SetIcon(GetResourceIcon(Player.ResourceType.Wheat));
                index++;
            }
            for (var i = 0; i < _player.Brick; i++)
            {
                _resourceCards[index].SetIcon(GetResourceIcon(Player.ResourceType.Brick));
                index++;
            }
            for (var i = 0; i < _player.Sheep; i++)
            {
                _resourceCards[index].SetIcon(GetResourceIcon(Player.ResourceType.Sheep));
                index++;
            }
        }

        private Sprite GetResourceIcon(Player.ResourceType type)
        {
            return resourceSprites.FirstOrDefault(t => t.resourceType == type).icon;
        }

        private void UpdateCardCount()
        {
            int count = _player.ResourceCount;
            if (_resourceCards.Count < count)
            {
                int currentCount = _resourceCards.Count;
                for (var i = 0; i < count - currentCount; i++)
                {
                    var card = Instantiate(cardPrefab, transform);
                    _resourceCards.Add(card.GetComponent<ResourceCard>());
                }
            } else if (_resourceCards.Count > count)
            {
                for (var i = 0; i < _resourceCards.Count - count; i++)
                {
                    Destroy(_resourceCards[i].gameObject);
                }
                _resourceCards.RemoveRange(0, _resourceCards.Count - count);
            }

            for (var i = 0; i < _resourceCards.Count; i++)
            {
                var card = _resourceCards[i];
                card.transform.SetSiblingIndex(i);
            }

            _lastHoveredCardIndex = count - 1;
        }

        private void UpdatePlayer()
        {
            if (_player) return;
            _player = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.GetComponent<Player>();
            _player.ResourcesUpdated += UpdateResourceCards;
            UpdateResourceCards();
        }
    }
}
