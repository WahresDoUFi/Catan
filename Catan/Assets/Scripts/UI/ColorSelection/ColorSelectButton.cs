using GamePlay;
using UnityEngine;
using UnityEngine.UI;
using User;

[RequireComponent(typeof(Button))]
public class ColorSelectButton : MonoBehaviour
{
    [SerializeField]
    private Image colorImage;
    [SerializeField]
    private GameObject blockedIcon;

    private Button _button;
    private int _colorIndex;

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    void Start()
    {
        _button.onClick.AddListener(ButtonClicked);
    }

    private void Update()
    {
        UpdateInteractableState();
        UpdateBlockedState();
    }

    public void SetColor(int colorIndex)
    {
        _colorIndex = colorIndex;
        colorImage.color = GameManager.Instance.GetColorById(colorIndex);
    }

    private void UpdateInteractableState()
    {
        foreach (var colorId in Player.GetUsedColorIds())
        {
            if (colorId == _colorIndex)
            {
                _button.interactable = false;
                return;
            }
        }
        _button.interactable = true;
    }

    private void UpdateBlockedState()
    {
        foreach (var colorId in Player.GetUsedColorIds(Player.LocalPlayer))
        {
            if (colorId == _colorIndex)
            {
                blockedIcon.SetActive(true);
                return;
            }
        }
        blockedIcon.SetActive(false);
    }

    private void ButtonClicked()
    {
        Player.LocalPlayer.TrySetColorId((byte)_colorIndex);
    }
}
