using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceDisplay : MonoBehaviour
{
    public Tile Resource => resource;

    [SerializeField] private Tile resource;
    [SerializeField] private TextMeshProUGUI amountText;

    public void SetAmount(int amount)
    {
        gameObject.SetActive(amount > 0);
        amountText.text = "x" + amount;
    }
}
