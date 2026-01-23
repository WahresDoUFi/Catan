using System.Linq;
using GamePlay;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class YearOfPlentySelection : MonoBehaviour
    {
        private static YearOfPlentySelection _instance;
        public static bool IsOpen => _instance.gameObject.activeSelf;

        [SerializeField] private Button confirmButton;
        
        private ResourceCounter[] _resourceCounter;

        private void Awake()
        {
            _instance = this;
            _resourceCounter = GetComponentsInChildren<ResourceCounter>();
            confirmButton.onClick.AddListener(SubmitResources);
            Close();
        }

        private void OnEnable()
        {
            foreach (var counter in _resourceCounter)
            {
                counter.Reset();
            }
        }

        private void Update()
        {
            UpdateResourceCounters();
        }

        public static void Open()
        {
            _instance.gameObject.SetActive(true);
        }

        public static void Close()
        {
            _instance.gameObject.SetActive(false);
        }

        private void UpdateResourceCounters()
        {
            int selected = _resourceCounter.Sum(counter => counter.Value);
            foreach (var counter in _resourceCounter)
            {
                counter.Limit = 2 - selected + counter.Value;
            }

            confirmButton.interactable = selected == 2;
        }

        private void SubmitResources()
        {
            var resources = (from counter in _resourceCounter where counter.Value > 0 
                select new BuildManager.ResourceCosts() 
                    { 
                        resource = counter.Resource,
                        amount = counter.Value 
                    }).ToArray();
            GameManager.Instance.SelectYearOfPlentyResources(resources);
        }
    }
}
