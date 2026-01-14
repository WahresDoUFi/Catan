using TMPro;
using UnityEngine;

public class Tooltip : MonoBehaviour
{
    [SerializeField] private Vector2 padding;
    [SerializeField] private float maxWidth;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform tooltipRectTransform;

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    public void SetTooltip(RectTransform target, string text)
    {
        gameObject.SetActive(true);
        tooltipText.text = text;
        var size = tooltipText.GetPreferredValues(maxWidth, 0);
        size.x = maxWidth;
        size += padding;
        _rectTransform.sizeDelta = size;
        var height = target.rect.yMax;
        _rectTransform.position = target.transform.TransformPoint(Vector3.up * height);
    }
}
