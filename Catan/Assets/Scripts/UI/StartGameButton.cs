using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class StartGameButton : MonoBehaviour
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private AudioSource lobbyMusic;

        private void Start()
        {
            startGameButton.onClick.AddListener(() =>
            {
                StartCoroutine(FadeOutMusic());
                GameManager.Instance.StartGame();
            });
        }

        private void Update()
        {
            if (!NetworkManager.Singleton
                || !NetworkManager.Singleton.IsConnectedClient
                || GameManager.Instance.State != GameManager.GameState.Waiting)
            {
                startGameButton.gameObject.SetActive(false);
                return;
            }

            startGameButton.gameObject.SetActive(true);
            startGameButton.interactable = NetworkManager.Singleton.IsHost;
        }

        private IEnumerator FadeOutMusic()
        {
            var startVolume = lobbyMusic.volume;
            const float fadeTime = 4.0f;

            while (lobbyMusic.volume > 0)
            {
                lobbyMusic.volume -= startVolume * Time.deltaTime / fadeTime;
                yield return null;
            }

            lobbyMusic.Stop();
            lobbyMusic.volume = startVolume;
        }
    }
}