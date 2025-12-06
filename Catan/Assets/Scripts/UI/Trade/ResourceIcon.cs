using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class ResourceIcon : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI amountText;

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void SetData(Tile resource, int amount)
        {
            _image.sprite = ResourceIconProvider.GetIcon(resource);
            amountText.text = "x" + amount;
        }
    }
}
