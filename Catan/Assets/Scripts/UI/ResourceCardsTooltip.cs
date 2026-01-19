using UI;
using UnityEngine;
using User;

namespace UI
{
    public class ResourceCardsTooltip : MonoBehaviour, IHoverable
    {
        [SerializeField]
        private GameObject tooltip;

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
        }

        public void Clicked()
        {
            tooltip.SetActive(!tooltip.activeSelf);
        }

        public void HoverUpdated(bool hovering)
        {
            tooltip.SetActive(hovering);
            UpdateTooltipContent();
        }

        private void UpdateTooltipContent()
        {
            foreach (var display in _resourceDisplays)
            {
                display.SetAmount(_player.GetResources(display.Resource));
            }
        }
    }
}