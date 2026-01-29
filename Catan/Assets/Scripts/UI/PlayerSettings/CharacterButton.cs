using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.PlayerSettings
{
    [RequireComponent(typeof(Button))]
    public class CharacterButton : MonoBehaviour
    {
        [SerializeField] private RectTransform mask;
        [SerializeField] private float selectedScale;
        [SerializeField] private float scaleSpeed;

        private Vector2 _defaultSize;
        private Button _button;
        private bool _selected;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _defaultSize = mask.sizeDelta;
        }

        private void Update()
        {
            var targetSize = _selected ? _defaultSize * selectedScale : _defaultSize;
            mask.sizeDelta = Vector2.Lerp(mask.sizeDelta, targetSize, Time.deltaTime * scaleSpeed);
        }

        public void AddListener(UnityAction callback)
        {
            _button.onClick.AddListener(callback);
        }

        public void SetSelected(bool selected)
        {
            _button.interactable = !selected;
            _selected = selected;
        }
    }
}