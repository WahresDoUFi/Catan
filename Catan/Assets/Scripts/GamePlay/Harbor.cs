using UnityEngine;
using Unity.Netcode;
using UI;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GamePlay
{
    public class Harbor : NetworkBehaviour
    {
        public static readonly List<Harbor> AllHarbors = new();

        public bool IsResourceTrade => resourceTrade;
        public Tile Resource => (Tile)_resource.Value;
        public Sprite TraderIcon => traderIcon;

        [SerializeField] private bool resourceTrade;
        [SerializeField] private Color iconColor;
        [SerializeField] private GameObject improvedTradeText;
        [SerializeField] private Sprite traderIcon;

        private readonly NetworkVariable<byte> _resource = new(byte.MaxValue);

        private Image _iconImage;

        private void Awake()
        {
            AllHarbors.Add(this);
        }

        public override void OnNetworkSpawn()
        {
            var icon = MapIconManager.AddIcon(transform, IconType.Harbor, iconColor);
            icon.Alpha = 0.5f;
            if (resourceTrade)
            {
                _iconImage = new GameObject("Icon").AddComponent<Image>();
                _iconImage.transform.SetParent(icon.transform, false);
                ResourceChanged();
                _resource.OnValueChanged += (_, _) => ResourceChanged();
            } else
            {
                Instantiate(improvedTradeText, icon.transform);
            }
        }

        public override void OnDestroy()
        {
            AllHarbors.Remove(this);
        }

        public void SetResource(Tile resource)
        {
            if (!NetworkManager.IsHost || !resourceTrade) return;
            _resource.Value = (byte)resource;
        }

        private void ResourceChanged()
        {
            _iconImage.sprite = ResourceDataProvider.GetIcon((Tile)_resource.Value);
        }
    }
}
