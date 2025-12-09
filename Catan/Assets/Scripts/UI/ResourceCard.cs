using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class ResourceCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public bool Hover { get; private set; }
        public Tile ResourceType => _type;
        public event Action OnClick;
        
        [SerializeField] private Image icon;
        [SerializeField] private float scaleSpeed;
        [SerializeField] private float defaultScale = 0.5f;
        [SerializeField] private float hoverScale = 1f;

        private Vector3 _size = Vector3.one;
        private Coroutine _scaleCoroutine;
        private bool _revealingCard;
        private Tile _type;

        public void SetType(Tile type)
        {
            icon.sprite = ResourceIconProvider.GetIcon(type);
            _type = type;
        }

        public void ToggleIconVisibility(bool visible)
        {
            icon.gameObject.SetActive(visible);
            _size = visible ? Vector3.one : new Vector3(-1f, 1f);
            var scale = transform.localScale;
            scale.x = visible ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        public void Reveal(Action onComplete = null)
        {
            ToggleIconVisibility(false);
            StartCoroutine(RevealCard(1f, onComplete));
        }

        private IEnumerator RevealCard(float time, Action onComplete)
        {
            _revealingCard = true;
            yield return StartCoroutine(AnimateScale(hoverScale, GetProgressTowards(hoverScale, defaultScale) * scaleSpeed));
            var t = 0f;
            //  Flip card 0.5
            var scale = transform.localScale;
            while (t < time / 2f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(scale, new Vector3(0f, hoverScale), t / time);
                yield return null;
            }
            // Flip card to normal direction + show icon
            t = 0f;
            while (t < time / 2f)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(new Vector3(0f, hoverScale), Vector3.one * hoverScale, t / time);
                yield return null;
            }
            _revealingCard = false;
            onComplete?.Invoke();
        }

        private void Scale(float target, float from)
        {
            if (_revealingCard) return;
            if (_scaleCoroutine != null)
                StopCoroutine(_scaleCoroutine);
            _scaleCoroutine = StartCoroutine(AnimateScale(target, GetProgressTowards(target, from) * scaleSpeed));
        }

        private float GetProgressTowards(float to, float from)
        {
            float currentScale = transform.localScale.x;
            return Mathf.InverseLerp(to, from, currentScale);
        }

        private IEnumerator AnimateScale(float targetScale, float time)
        {
            var t = 0f;
            Vector3 startScale = transform.localScale;
            while (t < time)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(startScale, _size * targetScale, t / time);
                yield return null;
            }
            transform.localScale = _size * targetScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hover = true;
            Scale(hoverScale, defaultScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hover = false;
            Scale(defaultScale, hoverScale);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }
    }
}