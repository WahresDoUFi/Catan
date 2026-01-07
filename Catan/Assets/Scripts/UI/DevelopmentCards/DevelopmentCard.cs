using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.DevelopmentCards
{
    public class DevelopmentCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public enum Type
        {
            VictoryPoint,
            Knight,
            RoadBuilding,
            YearOfPlenty,
            Monopoly,
            HangedKnights
        }

        public event Action<DevelopmentCard> CardClicked;
        public bool IsHovered => _isHovering;
        public bool Revealed { get; private set; }
        public Type CardType { get; private set; }

        public float Scale
        {
            get => Mathf.InverseLerp(0f, 1f, transform.localScale.x);
            set => transform.localScale = Vector3.one * Mathf.Clamp(value, 0, 1);
        }

        [SerializeField] private Image icon;
        [SerializeField] private float revealTime;
        [SerializeField] private float revealScale;
        [SerializeField] private float revealFinishDelay;

        private bool _isHovering;

        public void SetType(Type type, bool revealed = false)
        {
            Revealed = revealed;
            CardType = type;
            icon.sprite = DevelopmentCardIconProvider.GetIcon(type);
            icon.gameObject.SetActive(revealed);
            transform.localScale = revealed ? Vector3.one : new Vector3(-1f, 1f);
        }

        public void RevealCard(Action callback)
        {
            StartCoroutine(AnimateRevealCard(callback));
        }

        private IEnumerator AnimateRevealCard(Action callback)
        {
            float stepTime = revealTime / 2;
            var targetScale = transform.localScale;
            var t = 0f;
            icon.gameObject.SetActive(false);
            while (t < stepTime)
            {
                t += Time.deltaTime;
                targetScale.x = Mathf.Lerp(-1f, 0f, t / stepTime);
                targetScale.y = Mathf.Lerp(1f, revealScale, t / stepTime);
                transform.localScale = targetScale;
                yield return null;
            }
            icon.gameObject.SetActive(true);
            t = 0f;
            while (t < stepTime)
            {
                t += Time.deltaTime;
                targetScale.x = Mathf.Lerp(0f, 1f, t / stepTime);
                targetScale.y = Mathf.Lerp(revealScale, 1f, t / stepTime);
                transform.localScale = targetScale;
                yield return null;
            }
            Scale = 1f;
            yield return new WaitForSeconds(revealFinishDelay);

            Revealed = true;
            callback?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            CardClicked?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
        }
    }
}
