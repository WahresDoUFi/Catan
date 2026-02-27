using UI;
using UnityEngine;
using User;

namespace UI
{
    public class ResourceCardsTooltip : MonoBehaviour, IHoverable
    {
        [SerializeField] private GameObject tooltip;
        [SerializeField] private RectTransform contentTransform;
        
        private Player _player;
        private ResourceDisplay[] _resourceDisplays;

        private void Awake()
        {
            _resourceDisplays = GetComponentsInChildren<ResourceDisplay>();
        }

        private void Start()
        {
            tooltip.SetActive(false);
        }

        public void SetPlayer(Player player)
        {
            _player = player;
            _player.ResourcesUpdated += UpdateTooltipContent;
            UpdateTooltipContent();
        }

        public void Clicked()
        {
            tooltip.SetActive(!tooltip.activeSelf);
        }

        public void HoverUpdated(bool hovering)
        {
            tooltip.SetActive(hovering);
            CheckVisibility();
        }

        private void UpdateTooltipContent()
        {
            tooltip.transform.localRotation = Quaternion.identity;
            contentTransform.localRotation = Quaternion.identity;
            foreach (var display in _resourceDisplays)
            {
                display.SetAmount(_player.GetResources(display.Resource));
            }
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);
            CheckVisibility();
        }

        private void CheckVisibility()
        {
            var rect = contentTransform.rect;
            var screen = new Vector2(Screen.width, Screen.height);
            var max = contentTransform.TransformPoint(rect.max);
            bool invert = max.y > screen.y; // outside of the screen

            var rotation = invert ? Quaternion.Euler(0, 0, 180) : Quaternion.identity;
            tooltip.transform.localRotation = rotation;
            contentTransform.localRotation = rotation;
            contentTransform.anchoredPosition = invert ? new Vector2(0, contentTransform.sizeDelta.y) : Vector2.zero;
        }
    }
}