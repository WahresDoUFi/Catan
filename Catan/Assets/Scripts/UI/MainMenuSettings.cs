using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using User;
using AudioType = User.AudioType;

namespace UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MainMenuSettings : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Button settingsButton;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider soundEffectVolumeSlider;
        [SerializeField] private Button quitButton;
        [SerializeField] private float fadeSpeed;
        [SerializeField] private float settingsButtonOpenRotation;

        private bool _open;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            SetupBindings();
            SetInitialValues();
        }

        private void Update()
        {
            _canvasGroup.blocksRaycasts = _open;
            _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _open ? 1f : 0f, Time.deltaTime * fadeSpeed);
            float targetRotation = _open ? settingsButtonOpenRotation : 0f;
            float fromRotation = _open ? 0f : settingsButtonOpenRotation;
            var rotation = new Vector3(0f, 0f, Mathf.Lerp(fromRotation, targetRotation, _canvasGroup.alpha));
            settingsButton.transform.localEulerAngles = rotation;
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var rect = transform.GetChild(i).GetComponent<RectTransform>();
                if (RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position))
                    return;
            }
            Close();
        }

        private void SetInitialValues()
        {
            masterVolumeSlider.SetValueWithoutNotify(VolumeManager.GetMasterVolume());
            musicVolumeSlider.SetValueWithoutNotify(VolumeManager.GetVolume(AudioType.Music));
            soundEffectVolumeSlider.SetValueWithoutNotify(VolumeManager.GetVolume(AudioType.SoundEffect));
        }
        
        private void SetupBindings()
        {
            settingsButton.onClick.AddListener(Open);
            quitButton.onClick.AddListener(Application.Quit);
            masterVolumeSlider.onValueChanged.AddListener(VolumeManager.SetMasterVolume);
            musicVolumeSlider.onValueChanged.AddListener(volume => VolumeManager.SetVolume(AudioType.Music, volume));
            soundEffectVolumeSlider.onValueChanged.AddListener(volume => VolumeManager.SetVolume(AudioType.SoundEffect, volume));
        }

        private void Open()
        {
            _open = true;
        }

        private void Close()
        {
            _open = false;
        }
    }
}
