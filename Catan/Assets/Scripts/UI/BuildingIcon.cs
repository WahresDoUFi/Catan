using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public enum IconType
    {
        Street,
        Settlement,
        City
    }
    
    [RequireComponent(typeof(Image))]
    public class BuildingIcon : MonoBehaviour
    {
        [SerializeField] private float fadeSpeed;
        [SerializeField, Range(0f, 1f)] private float baseAlpha;
        
        public bool Visible { get; set; }
        private Transform _target;
        private Image _image;
        private Camera _mainCamera;
        private RectTransform _rectTransform;
        
        private void Awake()
        {
            _image = GetComponent<Image>();
            _mainCamera = Camera.main;
            _rectTransform = GetComponent<RectTransform>();
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
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        private void UpdateAlpha()
        {
            float targetAlpha = Visible ? baseAlpha : 0f;
            float alpha = Mathf.Lerp(_image.color.a, targetAlpha, Time.deltaTime * fadeSpeed);
            SetAlpha(alpha);
        }
        
        private void SetAlpha(float alpha)
        {
            var color = _image.color;
            color.a = alpha;
            _image.color = color;
        }

        private void UpdatePosition()
        {
            _rectTransform.position = _mainCamera.WorldToScreenPoint(_target.position);
        }
    }
}
