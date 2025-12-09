using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using Unity.Netcode;
using UnityEngine;
using User;

namespace UI
{
    public class ResourceCardsHolder : MonoBehaviour
    {
        private float VerticalOffset => BuildManager.BuildModeActive || GameManager.Instance.CanThrowDice() ? hiddenOffset : 0f;
        
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private float maxCardTilt;
        [SerializeField] private float maxCardOffset;
        [SerializeField] private float cardSpacing;
        [SerializeField] private float cardMoveSpeed = 5f;
        [SerializeField] private float hiddenOffset;
        
        private Player _player;
        
        private readonly List<ResourceCard> _resourceCards = new();
        private int _lastHoveredCardIndex;
        private void Update()
        {
            if (!NetworkManager.Singleton) return;
            if (!NetworkManager.Singleton.IsConnectedClient || GameManager.Instance.State != GameManager.GameState.Playing)
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
                    transform.position + Vector3.up * VerticalOffset, Time.deltaTime * cardMoveSpeed);
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
                float offset = i - (cardCount / 2f) + 0.5f;
                var targetPosition = Vector3.right * (offset * cardSpacing) + Vector3.up * VerticalOffset;
                float targetRotation = Mathf.Lerp(maxCardTilt, -maxCardTilt, (float)(i + 0.5f) / (float)cardCount);
                targetPosition -= Vector3.up * ((Mathf.Abs(targetRotation) / maxCardTilt) * maxCardOffset);
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
            for (var i = 0; i < _player.Sheep; i++)
            {
                _resourceCards[index].SetType(Tile.Grass);
                index++;
            }
            for (var i = 0; i < _player.Stone; i++)
            {
                _resourceCards[index].SetType(Tile.Stone);
                index++;
            }
            for (var i = 0; i < _player.Wood; i++)
            {
                _resourceCards[index].SetType(Tile.Forest);
                index++;
            }
            for (var i = 0; i < _player.Brick; i++)
            {
                _resourceCards[index].SetType(Tile.Brick);
                index++;
            }
            for (var i = 0; i < _player.Wheat; i++)
            {
                _resourceCards[index].SetType(Tile.Field);
                index++;
            }
        }

        private Tile GetMissingType()
        {
            if (_resourceCards.Count(card => card.ResourceType == Tile.Field) < _player.GetResources(Tile.Field))
                return Tile.Field;
            if (_resourceCards.Count(card => card.ResourceType == Tile.Grass) < _player.GetResources(Tile.Grass))
                return Tile.Grass;
            if (_resourceCards.Count(card => card.ResourceType == Tile.Brick) < _player.GetResources(Tile.Brick))
                return Tile.Brick;
            if (_resourceCards.Count(card => card.ResourceType == Tile.Forest) < _player.GetResources(Tile.Forest))
                return Tile.Forest;
            if (_resourceCards.Count(card => card.ResourceType == Tile.Stone) < _player.GetResources(Tile.Stone))
                return Tile.Stone;
            //  this part of the code should never be reached as this method is only supposed to be called
            //  when it was already checked that there are cards missing
            return Tile.Desert;
        }

        private int GetLastIndexOfType(Tile resourceType)
        {
            var resourceAsInt = (int)resourceType;
            var count = 0;
            for (var i = 0; i <= resourceAsInt; i++)
            {
                count += _player.GetResources((Tile)i);
            }
            return Mathf.Min(count, _resourceCards.Count + 1) - 1;
        }

        private void UpdateCardCount()
        {
            int count = _player.ResourceCount;
            if (_resourceCards.Count < count)
            {
                int currentCount = _resourceCards.Count;
                for (var i = 0; i < count - currentCount; i++)
                {
                    var card = Instantiate(cardPrefab, transform).GetComponent<ResourceCard>();
                    card.SetType(GetMissingType());
                    _resourceCards.Insert(GetLastIndexOfType(card.ResourceType), card);
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
