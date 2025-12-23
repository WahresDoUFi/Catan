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
        public static event Action LoadingDone;

        [SerializeField] private GameObject camObject;
        [SerializeField] private float fadeSpeed;
        [SerializeField] private Image progressBar;
        [SerializeField] private float progressBarAnimationSpeed;
        
        private CanvasGroup _canvasGroup;
        private AsyncOperation _status;
        private bool _setup;
        private bool _loadingDone;
        private bool _isSyncing;
        
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
            NetworkManager.Singleton.OnClientStarted += ClientConnected;
        }

        private static Task Show(AsyncOperation operation = null)
        {
            _instance.StartCoroutine(_instance.Fade(0f, 1f));
            _instance._status = operation;
            return Task.CompletedTask;
        }

        public static async Task PerformTasksInOrder(Action callback, params AsyncOperation[] tasks)
        {
            await Show();
            foreach (var task in tasks)
            {
               _instance._status = task;
               await task;
            }

            callback.Invoke();

            _instance.StartCoroutine(_instance.Fade(1f, 0f));
        }

        private void ClientConnected()
        {
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
                LoadingDone?.Invoke();
                StartCoroutine(Fade(1f, 0f));
            }
        }

        private void StartLoadScene(ulong clientid, string scenename, LoadSceneMode loadscenemode, AsyncOperation asyncoperation)
        {
            if (_isSyncing || clientid != NetworkManager.Singleton.LocalClientId) return;
            asyncoperation.allowSceneActivation = false;
            _loadingDone = false;
            _status = asyncoperation;
            StartCoroutine(FinishSceneLoading(asyncoperation));
            camObject.SetActive(true);
        }

        private IEnumerator FinishSceneLoading(AsyncOperation asyncoperation)
        {
            while (asyncoperation.progress < 0.9f)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.3f);
            asyncoperation.allowSceneActivation = true;
        }
        
        private void SceneLoadComplete(ulong clientid, string scenename, LoadSceneMode loadscenemode)
        {
            if (_isSyncing) return;
            if (clientid != NetworkManager.Singleton.LocalClientId) return;
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scenename));
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
