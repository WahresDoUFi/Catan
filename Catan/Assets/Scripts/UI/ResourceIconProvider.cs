using System;
using System.Linq;
using UnityEngine;

namespace UI
{
    [Serializable]
    public struct ResourceIcon
    {
        public Tile resourceType;
        public Sprite icon;
    }
    public class ResourceIconProvider : MonoBehaviour
    {
        private static ResourceIconProvider _instance;
        
        [SerializeField]
        private ResourceIcon[] resourceIcons;

        private void Awake()
        {
            _instance = this;
        }

        public static Sprite GetIcon(Tile resourceType)
        {
            return _instance.resourceIcons.FirstOrDefault(t => t.resourceType == resourceType).icon;
        }
    }
}