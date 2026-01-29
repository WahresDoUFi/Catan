using TMPro;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(TMP_InputField))]
    public class PlayerNameInputField : MonoBehaviour
    {
        private TMP_InputField _inputField;

        private void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();
            _inputField.onEndEdit.AddListener(OnEndEdit);
            _inputField.text = GetPlayerName();
            OnEndEdit(_inputField.text);
        }

        private string GetPlayerName()
        {
            return PlayerPrefs.GetString("Nickname", "Player#" + Random.Range(1000, 9999));
        }

        private void OnEndEdit(string text)
        {
            if (text.Length >= 3)
            {
                PlayerPrefs.SetString("Nickname", text);
            }
            _inputField.SetTextWithoutNotify(GetPlayerName());
        }
    }
}
