using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI
{
    public class MapIconManager : MonoBehaviour
    {
        [Serializable]
        private struct IconData
        {
            public IconType iconType;
            public Sprite sprite;
            public float size;
        }
        
        private static MapIconManager _instance;
        
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private IconData[] iconSprites;
        
        private readonly List<MapIcon> _buildingIcons = new();

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            foreach (var icon in _buildingIcons)
            {
                icon.Visible = CameraController.IsOverview;
            }
        }

        public static MapIcon AddIcon(Transform target, IconType type, Color color)
        {
            var icon = Instantiate(_instance.iconPrefab, _instance.transform).GetComponent<MapIcon>();
            _instance._buildingIcons.Add(icon);
            icon.SetTarget(target);
            icon.SetColor(color);
            var spriteData = GetSpriteDataForType(type);
            icon.SetSprite(spriteData.sprite);
            icon.SetSize(spriteData.size);
            return icon;
        }

        public static void UpdateIcon(MapIcon icon, IconType type)
        {
            icon.SetSprite(GetSpriteDataForType(type).sprite);
        }

        private static IconData GetSpriteDataForType(IconType type)
        {
            return _instance.iconSprites.FirstOrDefault(spriteInfo => spriteInfo.iconType == type);
        }
    }
}
