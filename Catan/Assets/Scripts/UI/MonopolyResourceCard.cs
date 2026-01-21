using GamePlay;
using TMPro;
using UI;
using UnityEngine;

public class MonopolyResourceCard : MonoBehaviour, IHoverable
{
    public Tile ResourceType => resourceType;

    [SerializeField] private Tile resourceType;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private float hoverScale;
    [SerializeField] private float animationSpeed;

    private bool _hovering;

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (_hovering ? hoverScale : 1f), Time.deltaTime * animationSpeed);
    }

    public void SetAmount(int amount)
    {
        amountText.text = "x" + amount;
    }

    public void Clicked()
    {
        GameManager.Instance.DeclareMonopoly(resourceType);
    }

    public void HoverUpdated(bool hovering)
    {
        _hovering = hovering;
    }
}
