using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class TabButton : MonoBehaviour
    {
        public Button.ButtonClickedEvent OnClick => _button.onClick;

        public bool Interactable
        {
            get => _button.interactable;
            set => _button.interactable = value;
        }
        
        [SerializeField] private TextMeshProUGUI text;
        
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }
        
        public void SetText(string newText) => text.text = newText;
    }
}