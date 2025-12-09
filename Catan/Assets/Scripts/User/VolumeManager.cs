using System;
using UnityEngine;

namespace User
{
    public static class VolumeManager
    {
        public delegate void VolumeChangeEvent();
        public static event VolumeChangeEvent OnVolumeChanged;
        
        public static void SetMasterVolume(float volume)
        {
            PlayerPrefs.SetFloat("MasterVolume", volume);
            OnVolumeChanged?.Invoke();
        }

        public static float GetMasterVolume()
        {
            return PlayerPrefs.GetFloat("MasterVolume", 1f);
        } 

        public static void SetVolume(AudioType type, float volume)
        {
            PlayerPrefs.SetFloat("Volume" + type.ToString(), volume);
            OnVolumeChanged?.Invoke();
        }

        public static float GetVolume(AudioType type)
        {
            return PlayerPrefs.GetFloat("Volume" + type.ToString(), 1f);
        }
    }
}
