using GamePlay;
using Networking;
using TMPro;
using UnityEngine;

namespace UI
{
    public class LobbyCodeDisplay : MonoBehaviour
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

        public static void CopyCodeToClipboard()
        {
            if (GameManager.Instance?.IsHost == true)
            {
                GUIUtility.systemCopyBuffer = MatchmakingManager.LobbyCode;
            }
        }
    }
}
