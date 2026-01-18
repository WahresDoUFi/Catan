using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopUpMessage : MonoBehaviour
{
    const float decayTime = 10f;

    public Vector2 Position
    {
        get => _rectTransform.anchoredPosition;
        set => _rectTransform.anchoredPosition = value;
    }
    public float Height => _rectTransform.sizeDelta.y;
    public float Alpha 
    {
        get => _canvasGroup.alpha;
        set => _canvasGroup.alpha = value;
    }

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Image timerDisplayImage;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button dismissButton;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionText;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private RectTransform _textRectTransform;
    private float _timer;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _textRectTransform = description.GetComponent<RectTransform>();
        actionButton.gameObject.SetActive(false);
    }

    private void Start()
    {
        _timer = decayTime;
        dismissButton.onClick.AddListener(Dismiss);
    }

    private void Update()
    {
        timerDisplayImage.fillAmount = _timer / decayTime;
        timerText.text = Mathf.CeilToInt(_timer) + "s";
        _timer -= Time.deltaTime;
        if (_timer < 0f)
        {
            Dismiss();
        }
    }

    public void SetTitle(string title)
    {
        titleText.text = title;
    }

    public void SetText(string text, float containerWidth)
    {
        description.text = text;
        var size = description.GetPreferredValues(containerWidth, 0);
        size.x = _rectTransform.sizeDelta.x;
        _textRectTransform.sizeDelta = size;
    }

    public void SetAction(string name, UnityAction action)
    {
        actionButton.gameObject.SetActive(true);
        actionText.text = name;
        actionButton.onClick.AddListener(action);
        actionButton.onClick.AddListener(Dismiss);
    }

    private void Dismiss()
    {
        enabled = false;
    }
}
