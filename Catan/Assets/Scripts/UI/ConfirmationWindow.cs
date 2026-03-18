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
        [SerializeField] private float fadeSpeed;
        [SerializeField] private float minWidth;
        [SerializeField] private float maxWidth;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Toggle dontShowAgainToggle;

        private bool _isOpen;
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            _instance = this;
            cancelButton.onClick.AddListener(Close);
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            _canvasGroup.interactable = _canvasGroup.blocksRaycasts = _isOpen;
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _isOpen ? 1f : 0f, Time.deltaTime * fadeSpeed);
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
            _instance._isOpen = true;
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
        }
    }
}
