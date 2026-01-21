using System;
using System.Linq;
using UnityEngine;

namespace UI
{
    [Serializable]
    public struct ResourceData
    {
        public string name;
        public Tile resourceType;
        public Sprite icon;
    }
    public class ResourceDataProvider : MonoBehaviour
    {
        private static ResourceDataProvider _instance;
        
        [SerializeField]
        private ResourceData[] resourceData;

        private void Awake()
        {
            _instance = this;
        }

        public static Sprite GetIcon(Tile resourceType)
        {
            return _instance.resourceData.FirstOrDefault(t => t.resourceType == resourceType).icon;
        }

        public static string GetResourceName(Tile resourceType)
        {
            return _instance.resourceData.FirstOrDefault(t => t.resourceType == resourceType).name;
        }
    }
}