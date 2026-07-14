using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ConfirmationWindow : MonoBehaviour
    {
        private static ConfirmationWindow _instance;
        public static bool IsOpen => _instance._isOpen;

        [SerializeField] private RectTransform windowFrameRectTransform;
        [SerializeField] private RectTransform contentRectTransform;
        [SerializeField] private float fadeTime;
        [SerializeField] private float minWidth;
        [SerializeField] private float maxWidth;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Toggle dontShowAgainToggle;
        [SerializeField] private float animationScale;

        private bool _isOpen;
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            _instance = this;
            cancelButton.onClick.AddListener(Close);
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.interactable = _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
        }

        public static void Show(string title, string text, UnityAction confirmAction = null, UnityAction<bool> dontShowAgainAction = null)
        {
            //  close any window that might still be open
            _instance.Close();

            _instance.dontShowAgainToggle.gameObject.SetActive(dontShowAgainAction != null);
            _instance.cancelButton.gameObject.SetActive(confirmAction != null);
            _instance.confirmButton.onClick.AddListener(() =>
            {
                confirmAction?.Invoke();
                dontShowAgainAction?.Invoke(_instance.dontShowAgainToggle.isOn);
                _instance.Close();
            });
            _instance.title.text = title;
            _instance.text.text = text;
            
            _instance.RecalculateSize();
            _instance.Open();
        }

        private void RecalculateSize()
        {
            float preferredWidth = text.GetPreferredValues().x + 60; // add some margin
            int maxLines = Mathf.CeilToInt(preferredWidth / minWidth);
            int minLines = Mathf.Max(1, Mathf.CeilToInt(preferredWidth / maxWidth));
            int addon = minLines / maxLines;
            float width = preferredWidth / (minLines + addon);
            var size = text.GetPreferredValues(width, 0);
            var textRect = text.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(Mathf.Clamp(width, minWidth, maxWidth), size.y);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
            windowFrameRectTransform.sizeDelta = contentRectTransform.rect.size + new Vector2(20f, 20f);
        }
        
        private void Close()
        {
            dontShowAgainToggle.onValueChanged.RemoveAllListeners();
            confirmButton.onClick.RemoveAllListeners();
            _isOpen = false;
            UITween.AnimateScale(this, 1f, animationScale, fadeTime);
            UITween.AnimateAlpha(_canvasGroup, 1f, 0f, fadeTime);
            _canvasGroup.interactable = _canvasGroup.blocksRaycasts = false;
        }

        private void Open()
        {
            transform.localScale = Vector3.one * animationScale;
            _canvasGroup.alpha = 0f;
            _isOpen = true;
            UITween.AnimateScale(this, animationScale, 1f, fadeTime);
            UITween.AnimateAlpha(_canvasGroup, 0f, 1f, fadeTime);
            UITween.DelayAction(this, fadeTime, () => _canvasGroup.interactable = _canvasGroup.blocksRaycasts = true);
        }
    }
}
