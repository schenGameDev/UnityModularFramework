using ModularFramework;
using UnityEngine;
using UnityEngine.Audio;

namespace UnityModularFramework
{
    [CreateAssetMenu(menuName = "Game Module/Volume System", fileName = "VolumeSystem_SO")]
    public class VolumeSystemSO : GameSystem
    {
        public enum VolumeType
        {
            MASTER,
            MUSIC,
            SFX
        }
        
        [Header("Config")] 
        public AudioMixerGroup masterGroup;
        public AudioMixerGroup musicGroup,soundFxGroup;
        
        #region SetVolume
        private void SetVolume(AudioMixerGroup mixerGroup, float volume) {
            mixerGroup.audioMixer.SetFloat(mixerGroup.name, Mathf.Log10(volume) * 20f);
        }

        public void SetMasterVolume(float volume) => SetVolume(masterGroup, volume);
        public void SetMusicVolume(float volume) => SetVolume(musicGroup, volume);
        public void SetSoundFxVolume(float volume) => SetVolume(soundFxGroup, volume);
        #endregion
        
        #region GetVolume
        private float GetVolume(AudioMixerGroup mixerGroup)
        {
            if (mixerGroup.audioMixer.GetFloat(mixerGroup.name, out var volume))
            {
                return Mathf.Clamp(Mathf.Pow(10, volume / 20f), 0, 1);
            }
            return 1;
        }
    
        public float GetMasterVolume() => GetVolume(masterGroup);
        public float GetMusicVolume() => GetVolume(musicGroup);
        public float GetSoundFxVolume() => GetVolume(soundFxGroup);
        #endregion
    }
}