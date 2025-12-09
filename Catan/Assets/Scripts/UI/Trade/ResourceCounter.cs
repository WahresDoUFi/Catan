using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using User;

namespace UI.Trade
{
    public class ResourceCounter : MonoBehaviour
    {
        public byte Value
        {
            get => (byte)_value;
            private set
            {
                if (!_player) _value = 0;
                else _value = Mathf.Clamp(value, 0, _player.GetResources(resource));
            }
        }

        public Tile Resource => resource;

        [SerializeField] private Tile resource;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Button addButton;
        [SerializeField] private Button removeButton;

        private int _value;
        private Player _player;

        private void OnEnable()
        {
            addButton.onClick.AddListener(AddResource);
            removeButton.onClick.AddListener(RemoveResource);
        }

        private void OnDisable()
        {
            addButton.onClick.RemoveAllListeners();
            removeButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            if (!_player) return;
            amountText.text = Value.ToString();
            UpdateButtonState();
        }

        public void Reset()
        {
            Value = 0;
        }

        public void SetPlayer(Player player)
        {
            _player = player;
            gameObject.SetActive(_player);
        }

        private void UpdateButtonState()
        {
            removeButton.interactable = Value > 0;
            addButton.interactable = Value < _player.GetResources(resource);
        }

        private void AddResource()
        {
            Value++;
        }

        private void RemoveResource()
        {
            Value--;
        }
    }
}
