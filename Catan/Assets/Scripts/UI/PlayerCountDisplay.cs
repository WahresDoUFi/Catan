using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UI
{
    public class PlayerCountDisplay : MonoBehaviour
    {
        private const string PlayerCountText = "Players: {0}/{1}";
        private TextMeshProUGUI _textField;

        private void Awake()
        {
            _textField = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient)
            {
                _textField.gameObject.SetActive(true);
                _textField.text = "Not connected";
                return;
            }
            _textField.gameObject.SetActive(GameManager.Instance.State == GameManager.GameState.Waiting);
            _textField.text = string.Format(PlayerCountText, GameManager.Instance.PlayerCount, GameManager.MaxPlayers);
        }
    }
}
