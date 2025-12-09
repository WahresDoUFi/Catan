using System;
using GamePlay;
using Networking;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UI
{
    public class PlayerCountDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _textField;

        private void Awake()
        {
            _textField = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            _textField.text = MatchmakingManager.LobbyCode;
        }
    }
}
