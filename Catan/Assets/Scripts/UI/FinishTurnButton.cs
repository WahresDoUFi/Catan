using GamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class FinishTurnButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Start()
        {
            button.onClick.AddListener(GameManager.Instance.FinishTurn);
            button.onClick.AddListener(ButtonClicked);
        }

        private void Update()
        {
            if (!GameManager.Instance) return;
            if (GameManager.Instance.IsMyTurn())
            {
                button.gameObject.SetActive(true);    
            }
            else
            {
                button.gameObject.SetActive(false);
                button.interactable = true;
            }
        }

        private void ButtonClicked()
        {
            //button.interactable = false;
        }
    }
}
