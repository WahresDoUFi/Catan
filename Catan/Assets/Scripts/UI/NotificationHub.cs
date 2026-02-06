using System.Collections.Generic;
using System.Linq;
using GamePlay;
using UI.Trade;
using UnityEngine;
using User;

namespace UI
{
    public class NotificationHub : MonoBehaviour
    {
        private const string AccentColor = "#79D2D6";
        private static NotificationHub _instance;

        [SerializeField] private GameObject popUpMessagePrefab;
        [SerializeField] private Vector2 spawnOffset;
        [SerializeField] private float messageMoveSpeed;
        [SerializeField] private float messageSpacing;

        private readonly List<PopUpMessage> _messages = new();

        private RectTransform _rectTransform;

        private void Awake()
        {
            _instance = this;
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            UpdatePopUps();
        }

        public static void TradeReceived(TradeInfo trade)
        {
            (string playerName, string hexColor) = GetPlayerData(GameManager.Instance.ActivePlayer);
            var message = SpawnPopUp("New Trade!", $"<color=#{hexColor}>{playerName}</color> sent you a trade offer");
            message.SetAction("View", () => TradeWindow.OpenWithMenu(1));
        }

        public static void KnightsHanged(byte amount)
        {
            (string playerName, string hexColor) = GetPlayerData(GameManager.Instance.ActivePlayer);
            SpawnPopUp("Knights Hanged!", $"<color=#{hexColor}>{playerName} has hanged <color={AccentColor}>{amount}</color> of your knights");
        }

        public static void ResourcesStolen(ulong playerId, Tile resource, byte amount)
        {
            (string playerName, string hexColor) = GetPlayerData(GameManager.Instance.ActivePlayer);
            SpawnPopUp("Resources stolen!", $"<color=#{hexColor}>{playerName}</color> stole <color={AccentColor}>x{amount} {ResourceDataProvider.GetResourceName(resource)}</color> from you");
        }

        public static void MonopolyDeclared(Tile resource)
        {
            (string playerName, string hexColor) = GetPlayerData(GameManager.Instance.ActivePlayer);
            SpawnPopUp("Monopoly Declared!", $"<color={hexColor}>{playerName}</color> declared a monopoly on <color={AccentColor}>{ResourceDataProvider.GetResourceName(resource)}</color>");
        }

        private static PopUpMessage SpawnPopUp(string title, string description)
        {
            var message = Instantiate(_instance.popUpMessagePrefab, _instance.transform).GetComponent<PopUpMessage>();
            message.Alpha = 0f;
            message.Position = _instance.spawnOffset;
            message.SetTitle(title);
            //  reduce width by 10 for padding
            message.SetText(description, _instance._rectTransform.sizeDelta.x - 10f);
            _instance._messages.Add(message);
            return message;
        }

        private static (string playerName, string playerColorHex) GetPlayerData(ulong playerId)
        {
            var playerName = Player.GetPlayerById(playerId).PlayerName;
            var playerColor = GameManager.Instance.GetPlayerColor(playerId);
            var hexColor = ColorUtility.ToHtmlStringRGB(playerColor);
            return (playerName, hexColor);
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
}
