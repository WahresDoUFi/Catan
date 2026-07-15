using GamePlay;
using UnityEngine;
using UnityEngine.UI;
using User;

public class ColorSelectMenu : MonoBehaviour
{
    [SerializeField]
    private ColorSelectButton buttonPrefab;
    [SerializeField]
    private Image playerIcon;
    [SerializeField]
    private Sprite[] profileSprites;

    private void Start()
    {
        for (var i = 0; i < GameManager.Instance.PlayerColors.Length; i++)
        {
            Instantiate(buttonPrefab, transform).SetColor(i);
        }
        playerIcon.sprite = profileSprites[Player.LocalPlayer.PictureId];
    }
}
