using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using User;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private Canvas gameOverScreen;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private Image gameOverImage;
        [SerializeField] private Image sparkles;
        [SerializeField] private Sprite winSprite;
        [SerializeField] private Sprite loseSprite;
        [SerializeField] private Button returnToMenuButton;
        [SerializeField] private AudioClip winSound;

        private CanvasGroup _canvasGroup;
        private bool _shouldShow;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            returnToMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void Update()
        {
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _shouldShow ? 1f : 0f, Time.deltaTime * 1f);
        }

        private void ReturnToMainMenu()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        public void ShowGameOverScreen(Player winner)
        {
            if (gameOverScreen != null)
            {
                if (winner.IsOwner)
                {
                    gameOverText.text = "You won! \nCongratulations!";
                    gameOverImage.sprite = winSprite;
                    sparkles.gameObject.SetActive(true);
                    gameOverImage.rectTransform.sizeDelta = (new Vector2(500, 500));
                    AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position, 0.15f);
                }
                else
                {
                    gameOverText.text = "You lost! Player " + winner.PlayerName + " won the game!";
                    gameOverImage.rectTransform.sizeDelta = (new Vector2(768, 552));
                    gameOverImage.sprite = loseSprite;
                }

                gameOverScreen.gameObject.SetActive(true);
                _canvasGroup.blocksRaycasts = true;
                _shouldShow = true;
            }
        }
    }
}