using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public enum IconType
    {
        Street,
        Settlement,
        City,
        Tile
    }
    
    [RequireComponent(typeof(Image))]
    public class MapIcon : MonoBehaviour
    {
        [SerializeField] private float fadeSpeed;
        [SerializeField, Range(0f, 1f)] private float baseAlpha;
        
        public bool Visible { get; set; }
        private Transform _target;
        private Image _image;
        private Camera _mainCamera;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            _image = GetComponent<Image>();
            _mainCamera = Camera.main;
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            UpdateAlpha();
            UpdatePosition();
        }

        public void SetColor(Color color)
        {
            _image.color = color;
        }
        public void SetSprite(Sprite sprite)
        {
            _image.sprite = sprite;
        }
        public void SetSize(float size)
        {
            _rectTransform.sizeDelta = new Vector2(size, size);
        }
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void UpdateAlpha()
        {
            float targetAlpha = Visible ? baseAlpha : 0f;
            float alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
            SetAlpha(alpha);
        }
        
        private void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = alpha;
        }

        private void UpdatePosition()
        {
            if (!_target) return;
            _rectTransform.position = _mainCamera.WorldToScreenPoint(_target.position);
        }
    }
}
