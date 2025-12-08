using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingScreen : MonoBehaviour
    {
        private static LoadingScreen _instance;

        [SerializeField] private GameObject camObject;
        [SerializeField] private float fadeSpeed;
        [SerializeField] private Image progressBar;
        [SerializeField] private float progressBarAnimationSpeed;
        
        private CanvasGroup _canvasGroup;
        private AsyncOperation _status;
        private bool _setup;
        private bool _loadingDone;
        
        private void Awake()
        {
            if (_instance)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(this);
            DontDestroyOnLoad(camObject);
            _instance = this;
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            camObject.SetActive(false);
        }

        private void Start()
        {
            ConnectionNotificationManager.Instance.OnClientConnectionNotification += ClientConnected;
        }

        public static async Task Show()
        {
            _instance.StartCoroutine(_instance.Fade(0f, 1f));
        }

        private void ClientConnected(ulong clientId, ConnectionNotificationManager.ConnectionStatus connectionStatus)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId) return;
            if (connectionStatus == ConnectionNotificationManager.ConnectionStatus.Connected)
                OnConnect(NetworkManager.Singleton);
        }

        private void OnConnect(NetworkManager networkManager)
        {
            networkManager.SceneManager.OnLoad += StartLoadScene;
            networkManager.SceneManager.OnLoadComplete += SceneLoadComplete;
            StartCoroutine(UnloadScene());
        }

        private void Update()
        {
            if (_status == null || _loadingDone)
            {
                progressBar.fillAmount = 0f;
                return;
            }
            progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, _status.progress, Time.deltaTime * progressBarAnimationSpeed);
            if (progressBar.fillAmount > 0.99f)
            {
                _loadingDone = true;
                progressBar.fillAmount = 1f;
                StartCoroutine(Fade(1f, 0f));
            }
        }

        private void GameJoined(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType == ConnectionEvent.ClientConnected)
                OnConnect(NetworkManager.Singleton);
        }

        private void StartLoadScene(ulong clientid, string scenename, LoadSceneMode loadscenemode, AsyncOperation asyncoperation)
        {
            if (clientid != NetworkManager.Singleton.LocalClientId) return;
            _loadingDone = false;
            _status = asyncoperation;
            camObject.SetActive(true);
        }
        
        private void SceneLoadComplete(ulong clientid, string scenename, LoadSceneMode loadscenemode)
        {
            if (clientid != NetworkManager.Singleton.LocalClientId) return;
            camObject.SetActive(false);
        }

        private IEnumerator UnloadScene()
        {
            yield return SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
            yield return Fade(0f, 1f);
        }

        private IEnumerator Fade(float start, float end)
        {
            _canvasGroup.alpha = start;
            var t = 0f;
            while (t < fadeSpeed)
            {
                t += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(start, end, t / fadeSpeed);
                yield return null;
            }
            _canvasGroup.alpha = end;
        }
    }
}
