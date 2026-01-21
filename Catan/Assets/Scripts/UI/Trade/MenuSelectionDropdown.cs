using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Trade
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MenuSelectionDropdown : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private Button[] buttons;
        [SerializeField] private GameObject[] menus;
        
        private CanvasGroup _canvasGroup;
        
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void Start()
        {
            ConnectButtons();
        }

        public void Open()
        {
            StopAllCoroutines();
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            StartCoroutine(Fade(1f, transitionDuration));
        }

        public void SelectMenu(int index)
        {
            for (var i = 0; i < menus.Length; i++)
            {
                buttons[i].interactable = i != index;
                menus[i].SetActive(i == index);
            }

            Close();
        }

        private void Close()
        {
            StopAllCoroutines();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            StartCoroutine(Fade(0f, transitionDuration));
        }

        private void ConnectButtons()
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                int index = i;
                buttons[i].onClick.AddListener(() => SelectMenu(index));
            }
        }

        private IEnumerator Fade(float target, float duration)
        {
            float current = _canvasGroup.alpha;
            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(current, target, t / duration);
                yield return null;
            }
            _canvasGroup.alpha = target;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Close();
        }
    }
}
