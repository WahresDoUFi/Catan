using System;
using System.Linq;
using UnityEngine;

namespace UI.DevelopmentCards
{
    public class DevelopmentCardIconProvider : MonoBehaviour
    {
        [Serializable]
        public struct IconType
        {
            public DevelopmentCard.Type cardType;
            public Sprite icon;
        }
        
        private static DevelopmentCardIconProvider _instance;

        [SerializeField] private IconType[] icons;
        
        private void Awake()
        {
            _instance = this;
        }

        public static Sprite GetIcon(DevelopmentCard.Type cardType)
        {
            return _instance.icons.FirstOrDefault(card => card.cardType == cardType).icon;
        }
    }
}