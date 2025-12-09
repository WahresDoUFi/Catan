using System;
using UnityEngine;

namespace User
{
    public class VolumeManager : MonoBehaviour
    {
        public static VolumeManager Instance;
        
        public delegate void VolumeChangeEvent(AudioType type, float volume);
        public event VolumeChangeEvent OnVolumeChanged;
        
        private void Awake()
        {
            Instance = this;
        }

        public void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat("MasterVolume", volume);
            OnVolumeChanged?.Invoke(AudioType.Music, GetVolume(AudioType.Music));
            OnVolumeChanged?.Invoke(AudioType.SoundEffect, GetVolume(AudioType.SoundEffect));
        }

        public float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat("MasterVolume", 1f);
        } 

        public void SetVolume(AudioType type, float volume)
        {
            PlayerPrefs.SetFloat("Volume" + type.ToString(), volume);
            OnVolumeChanged?.Invoke(type, volume);
        }

        public float GetVolume(AudioType type)
        {
            return PlayerPrefs.GetFloat("Volume" + type.ToString(), 1f) * GetMasterVolume();
        }
    }
}
