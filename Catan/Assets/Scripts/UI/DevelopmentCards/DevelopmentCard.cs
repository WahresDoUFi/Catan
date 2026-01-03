using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.DevelopmentCards
{
    public class DevelopmentCard : MonoBehaviour
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

        public bool Revealed { get; private set; }
        public Type CardType { get; private set; }

        public float Scale
        {
            get => Mathf.InverseLerp(0f, 1f, transform.localScale.x);
            set => transform.localScale = Vector3.one * Mathf.Clamp(value, 0, 1);
        }

        [SerializeField] private Image icon;
        [SerializeField] private float revealTime;
        [SerializeField] private float hoverScale;

        public void SetType(Type type)
        {
            CardType = type;
            icon.sprite = DevelopmentCardIconProvider.GetIcon(type);
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
                targetScale.y = Mathf.Lerp(1f, hoverScale, t / stepTime);
                transform.localScale = targetScale;
                yield return null;
            }
            icon.gameObject.SetActive(true);
            while (t < stepTime)
            {
                t += Time.deltaTime;
                targetScale.x = Mathf.Lerp(0f, 1f, t / stepTime);
                targetScale.y = Mathf.Lerp(hoverScale, 1f, t / stepTime);
                transform.localScale = targetScale;
                yield return null;
            }

            Revealed = true;
            transform.localScale = Vector3.one;
            callback?.Invoke();
        }
    }
}
