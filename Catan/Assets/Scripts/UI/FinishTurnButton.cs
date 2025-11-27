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
        }

        private void Update()
        {
            button.gameObject.SetActive(GameManager.Instance.IsMyTurn());
        }
    }
}
