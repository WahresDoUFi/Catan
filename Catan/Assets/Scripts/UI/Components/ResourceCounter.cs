using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components
{
    public class ResourceCounter : MonoBehaviour
    {
        public byte Value
        {
            get => (byte)_value;
            set => _value = Mathf.Clamp(value, 0, Limit);
        }
		public int Limit 
        { 
            get => _limit;
            set
            {
                if (_limit == value) return;
                _limit = value;
                _value = Mathf.Clamp(_value, 0, Limit);
                UpdateButtonState();
            }
        }
        public Tile Resource => resource;

        [SerializeField] private Tile resource;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private Button addButton;
        [SerializeField] private Button removeButton;

        private int _limit;
        private int _value;

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

        public void Reset()
        {
            Value = 0;
            UpdateButtonState();
        }

        protected virtual void UpdateButtonState()
        {
            amountText.text = Value.ToString();
            removeButton.interactable = Value > 0;
            addButton.interactable = Value < Limit;
        }

        private void AddResource()
        {
            Value++;
            UpdateButtonState();
        }

        private void RemoveResource()
        {
            Value--;
            UpdateButtonState();
        }
    }
}