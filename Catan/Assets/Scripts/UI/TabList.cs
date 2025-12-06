using UnityEngine;
using UnityEngine.UI;

namespace UI.Trade
{
    public class TabList : MonoBehaviour
    {
        [SerializeField] private Button[] tabButtons;
        [SerializeField] private GameObject[] tabs;

        private void Awake()
        {
            for (var i = 0; i < tabButtons.Length; i++)
            {
                int index = i;
                tabButtons[i].onClick.AddListener(() => OpenTab(index));
            }
            OpenTab(1);
        }

        private void OpenTab(int index)
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                tabButtons[i].interactable = i != index;
                tabs[i].SetActive(i == index);
            }
        }
    }
}
