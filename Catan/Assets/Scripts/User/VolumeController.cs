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
        public float Volume => _baseVolume;
        
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
            VolumeManager.OnVolumeChanged += UpdateVolume;
        }
        
        private void OnDestroy()
        {
            VolumeManager.OnVolumeChanged -= UpdateVolume;
        }

        public void SetBaseVolume(float baseVolume)
        {
            _baseVolume = baseVolume;
            UpdateVolume();
        }

        private void UpdateVolume()
        {
            _audioSource.volume = _baseVolume * VolumeManager.GetVolume(audioType) * VolumeManager.GetMasterVolume();
        }
    }
}
