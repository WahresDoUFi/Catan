using System;
using UnityEngine;

namespace User
{
    public class VolumeManager : MonoBehaviour
    {
        public static VolumeManager Instance;
        
        public delegate void VolumeChangeEvent();
        public event VolumeChangeEvent OnVolumeChanged;
        
        private void Awake()
        {
            Instance = this;
        }

        public void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat("MasterVolume", volume);
            OnVolumeChanged?.Invoke();
        }

        public float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat("MasterVolume", 1f);
        } 

        public void SetVolume(AudioType type, float volume)
        {
            PlayerPrefs.SetFloat("Volume" + type.ToString(), volume);
            OnVolumeChanged?.Invoke();
        }

        public float GetVolume(AudioType type)
        {
            return PlayerPrefs.GetFloat("Volume" + type.ToString(), 1f);
        }
    }
}
