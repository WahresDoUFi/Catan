using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using AudioType = User.AudioType;
using VolumeManager = User.VolumeManager;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class PauseMenu : MonoBehaviour
    {
        private static PauseMenu _instance;
        public static bool IsOpen => _instance._open;

        [SerializeField] private float fadeSpeed;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider soundEffectsVolumeSlider;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button quitButton;
        
        private CanvasGroup _canvasGroup;
        private bool _open;

        private void Awake()
        {
            _instance = this;
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            _open = false;
            _canvasGroup.alpha = 0f;
            LoadInitialValues();
            SetupBindings();
        }

        private void LoadInitialValues()
        {
            masterVolumeSlider.SetValueWithoutNotify(VolumeManager.Instance.GetMasterVolume());
            musicVolumeSlider.SetValueWithoutNotify(VolumeManager.Instance.GetVolume(AudioType.Music));
            soundEffectsVolumeSlider.SetValueWithoutNotify(VolumeManager.Instance.GetVolume(AudioType.SoundEffect));
        }

        private void SetupBindings()
        {
            masterVolumeSlider.onValueChanged.AddListener(volume => VolumeManager.Instance.SetMasterVolume(volume));
            musicVolumeSlider.onValueChanged.AddListener(volume => VolumeManager.Instance.SetVolume(AudioType.Music, volume));
            soundEffectsVolumeSlider.onValueChanged.AddListener(volume => VolumeManager.Instance.SetVolume(AudioType.SoundEffect, volume));
            closeButton.onClick.AddListener(Toggle);
            quitButton.onClick.AddListener(() => NetworkManager.Singleton.Shutdown());
        }

        private void Update()
        {
            _canvasGroup.blocksRaycasts = _open;
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _open ? 1f : 0f, Time.deltaTime * fadeSpeed);
        }

        public static void Toggle()
        {
            _instance._open = !_instance._open;
        }
    }
}
