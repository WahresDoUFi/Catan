using GamePlay;
using Unity.Netcode;
using UnityEngine;
using User;

public class MonopolySelection : MonoBehaviour
{
    private static MonopolySelection _instance;
    public static bool IsOpen => _instance.gameObject.activeSelf;

    [SerializeField] private Transform cardList;
    [SerializeField] private Vector2 minMaxSize;

    private MonopolyResourceCard[] _cards;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _instance = this;
        _rectTransform = GetComponent<RectTransform>();
        _cards = cardList.GetComponentsInChildren<MonopolyResourceCard>();
        gameObject.SetActive(false);
    }

    public static void Open()
    {
        _instance.gameObject.SetActive(true);
        _instance.UpdateCards();
    }

    public static void Close()
    {
        _instance.gameObject.SetActive(false);
    }

    private void UpdateCards()
    {
        var localClientId = NetworkManager.Singleton.LocalClientId;
        byte cardsActive = 0;
        foreach (var card in _cards)
        {
            byte resources = 0;
            foreach (var playerId in GameManager.Instance.GetPlayerIds())
            {
                if (playerId == localClientId) continue;

                resources += Player.GetPlayerById(playerId).GetResources(card.ResourceType);
            }

            if (resources > 0)
            {
                card.gameObject.SetActive(true);
                card.SetAmount(resources);
                cardsActive++;
            } else
            {
                card.gameObject.SetActive(false);
            }
        }

        var size = _rectTransform.sizeDelta;
        size.x = Mathf.Lerp(minMaxSize.x, minMaxSize.y, cardsActive / 5f);
        _rectTransform.sizeDelta = size;
    }
}
