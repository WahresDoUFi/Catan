using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    internal class HoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private GameObject targetObject;

        IHoverable target;

        private void Awake()
        {
            target = targetObject.GetComponent<IHoverable>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            target.Clicked();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            target.HoverUpdated(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            target.HoverUpdated(false);
        }
    }
}
