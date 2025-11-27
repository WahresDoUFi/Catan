using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class StartGameButton : MonoBehaviour
    {
        [SerializeField]
        private Button startGameButton;

        private void Start()
        {
            startGameButton.onClick.AddListener(GameManager.Instance.StartGame);
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
    }
}
