using UI.PlayerSettings;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelection : MonoBehaviour
{
    private CharacterButton[] _characterButtons;

    private void Start()
    {
        _characterButtons = GetComponentsInChildren<CharacterButton>();
        for (int i = 0; i < _characterButtons.Length; i++)
        {
            var index = i;
            _characterButtons[i].AddListener(() => SelectCharacter(index));
        }
        SelectCharacter(PlayerPrefs.GetInt("Character", Random.Range(0, _characterButtons.Length)));
    }

    private void SelectCharacter(int index)
    {
        for (int i = 0; i < _characterButtons.Length; i++)
        {
            var button = _characterButtons[i];
            button.SetSelected(i == index);
        }
        PlayerPrefs.SetInt("Character", index);
    }
}
