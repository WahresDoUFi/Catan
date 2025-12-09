using System;
using UnityEngine;

namespace User
{
    public enum AudioType
    {
        Music,
        SoundEffect
    }
    [RequireComponent(typeof(AudioSource))]
    public class VolumeController : MonoBehaviour
    {
        [SerializeField] private AudioType audioType;

        private float _baseVolume;
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _baseVolume = _audioSource.volume;
        }

        private void Start()
        {
            UpdateVolume();
            VolumeManager.Instance.OnVolumeChanged += UpdateVolume;
        }
        
        private void OnDestroy()
        {
            VolumeManager.Instance.OnVolumeChanged -= UpdateVolume;
        }

        private void UpdateVolume()
        {
            _audioSource.volume = _baseVolume * VolumeManager.Instance.GetVolume(audioType) * VolumeManager.Instance.GetMasterVolume();
        }
    }
}
