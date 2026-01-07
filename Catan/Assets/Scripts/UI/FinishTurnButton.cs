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
            if (IsActive())
            {
                button.gameObject.SetActive(true);    
            }
            else
            {
                button.gameObject.SetActive(false);
                button.interactable = true;
            }
        }

        private bool IsActive()
        {
            if (!GameManager.Instance.IsMyTurn()) return false;
            if (!GameManager.Instance.DiceThrown) return false;
            if (GameManager.Instance.CardLimitActive) return false;
            if (GameManager.Instance.RepositionBandit) return false;

            return true;
        }

        private void ButtonClicked()
        {
            button.interactable = false;
        }
    }
}
