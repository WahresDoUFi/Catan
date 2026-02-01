using UI;
using UnityEngine;
using User;

namespace UI
{
    public class ResourceCardsTooltip : MonoBehaviour, IHoverable
    {
        [SerializeField] private GameObject tooltip;
        [SerializeField] private Transform contentTransform;

        private RectTransform _rectTransform;
        private Player _player;
        private ResourceDisplay[] _resourceDisplays;

        private void Awake()
        {
            _resourceDisplays = GetComponentsInChildren<ResourceDisplay>();
            _rectTransform = tooltip.GetComponent<RectTransform>();
        }

        private void Start()
        {
            tooltip.SetActive(false);
        }

        public void SetPlayer(Player player)
        {
            _player = player;
            _player.ResourcesUpdated += UpdateTooltipContent;
        }

        public void Clicked()
        {
            tooltip.SetActive(!tooltip.activeSelf);
        }

        public void HoverUpdated(bool hovering)
        {
            tooltip.SetActive(hovering);
        }

        private void UpdateTooltipContent()
        {
            tooltip.transform.localRotation = Quaternion.identity;
            contentTransform.localRotation = Quaternion.identity;
            foreach (var display in _resourceDisplays)
            {
                display.SetAmount(_player.GetResources(display.Resource));
            }
            CheckVisibility();
        }

        private void CheckVisibility()
        {
            var rect = _rectTransform.rect;
            var screen = new Vector2(Screen.width, Screen.height);
            var max = tooltip.transform.TransformPoint(rect.max);
            if (max.y > screen.y)
            {
                tooltip.transform.up = Vector2.down;
                contentTransform.up = Vector2.up;
            }
        }
    }
}