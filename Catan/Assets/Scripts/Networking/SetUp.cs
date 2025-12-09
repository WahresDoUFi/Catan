using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Networking
{
    public class SetUp : MonoBehaviour
    {
        
#if UNITY_EDITOR
        public UnityEditor.SceneAsset loadingScene;
    
        private void OnValidate()
        {
            loadingSceneName = loadingScene.name;
        }
#endif
        [SerializeField] private string loadingSceneName;
        [SerializeField] private GameObject eventSystemPrefab;
        [SerializeField] GameObject networkManagerPrefab;
        private void Start()
        {
            if (NetworkManager.Singleton) return;
            var networkManager = Instantiate(networkManagerPrefab).GetComponent<NetworkManager>();
            networkManager.SetSingleton();
            SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Additive);
            DontDestroyOnLoad(Instantiate(eventSystemPrefab));
        }
    }
}