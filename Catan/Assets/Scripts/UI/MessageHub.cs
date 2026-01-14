using System.Collections.Generic;
using System.Linq;
using UI.Trade;
using UnityEngine;
using User;

public class MessageHub : MonoBehaviour
{
    private static MessageHub _instance;

    [SerializeField] private GameObject popUpMessagePrefab;
    [SerializeField] private Vector2 spawnOffset;
    [SerializeField] private float messageMoveSpeed;
    [SerializeField] private float messageSpacing;

    private readonly List<PopUpMessage> _messages = new();

    private void Awake()
    {
        _instance = this;
    }

    private void Update()
    {
        UpdatePopUps();
    }

    public static void TradeReceived(TradeInfo trade)
    {
        var message = Instantiate(_instance.popUpMessagePrefab, _instance.transform).GetComponent<PopUpMessage>();
        message.Alpha = 0f;
        message.Position = _instance.spawnOffset;
        message.SetTitle("New Trade!");
        message.SetText($"<color=#79D2D6>{Player.GetPlayerById(trade.SenderId).PlayerName}</color> sent you a trade offer");
        message.SetAction("View", () => TradeWindow.OpenWithMenu(1));
        _instance._messages.Add(message);
    }

    private void UpdatePopUps()
    {
        int activeCount = _messages.Count(message => message.enabled);
        float height = 0f;
        for (int i = _messages.Count - 1; i >= 0; i--)
        {
            PopUpMessage message = _messages[i];
            if (message.enabled)
            {
                message.Alpha = Mathf.Lerp(message.Alpha, 1f, Time.deltaTime * messageMoveSpeed);
                message.Position = Vector2.Lerp(message.Position, new Vector2(0f, height), Time.deltaTime * messageMoveSpeed);
                height -= message.Height + messageSpacing;
            } else
            {
                message.Position = Vector2.Lerp(message.Position, new Vector2(spawnOffset.x, message.Position.y), Time.deltaTime * messageMoveSpeed);
                message.Alpha = Mathf.Lerp(message.Alpha, 0f, Time.deltaTime * messageMoveSpeed);
                if (message.Alpha < 0.1f)
                {
                    Destroy(message.gameObject);
                    _messages.Remove(message);
                }
            }
        }
    }
}
