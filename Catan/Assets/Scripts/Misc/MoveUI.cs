using UnityEngine;

public class MoveUI : MonoBehaviour
{
    [SerializeField] private Vector3 moveDir;
    [SerializeField] private float moveSpeed;

    private RectTransform _rectTransform;
    private Vector3 _startPos;
    private bool _open;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _startPos = _rectTransform.anchoredPosition;
    }

    private void Update()
    {
        var targetPosition = _open ? _startPos + moveDir : _startPos;
        _rectTransform.anchoredPosition = Vector3.Lerp(_rectTransform.anchoredPosition, targetPosition, Time.deltaTime * moveSpeed);
    }

    public void Toggle()
    {
        SetOpen(!_open);
    }

    public void SetOpen(bool open)
    {
        _open = open;
    }
}
