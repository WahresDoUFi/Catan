using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI
{
    public class BuildingIconManager : MonoBehaviour
    {
        [Serializable]
        private struct IconSprite
        {
            public IconType iconType;
            public Sprite sprite;
        }
        
        private static BuildingIconManager _instance;
        
        [SerializeField] private GameObject iconPrefab;
        [SerializeField] private IconSprite[] iconSprites;
        
        private readonly List<BuildingIcon> _buildingIcons = new();

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

        public static void AddIcon(Transform target, IconType type, Color color)
        {
            var icon = Instantiate(_instance.iconPrefab, _instance.transform).GetComponent<BuildingIcon>();
            _instance._buildingIcons.Add(icon);
            icon.SetTarget(target);
            icon.SetColor(color);
            icon.SetSprite(_instance.iconSprites.FirstOrDefault(spriteInfo => spriteInfo.iconType == type).sprite);
        }
    }
}
