using ModularFramework;
using UnityEngine;
using UnityEngine.UI;
using VolumeType = UnityModularFramework.VolumeSystemSO.VolumeType;

namespace UnityModularFramework {
    [RequireComponent(typeof(Slider))]
    public class VolumeController : MonoBehaviour
    {
        [SerializeField] private VolumeType volumeType;

        private Slider _slider;
        private VolumeSystemSO _volumeSystem;

        private void Start()
        {
            _slider = GetComponent<Slider>();
            _volumeSystem = GameRunner.GetSystem<VolumeSystemSO>().Get();
            InitializeVolume();
        }

        private void OnEnable()
        {
            if (!didStart) return;
            InitializeVolume();
        }

        private void InitializeVolume()
        {
            switch (volumeType)
            {
                case VolumeType.MASTER:
                    _slider.value = _volumeSystem.GetMasterVolume();
                    break;
                case VolumeType.MUSIC:
                    _slider.value = _volumeSystem.GetMusicVolume();
                    break;
                case VolumeType.SFX:
                    _slider.value = _volumeSystem.GetSoundFxVolume();
                    break;
            }
        }

        public void SetVolume(float volume)
        {
            switch (volumeType)
            {
                case VolumeType.MASTER:
                    _volumeSystem.SetMasterVolume(volume);
                    break;
                case VolumeType.MUSIC:
                    _volumeSystem.SetMusicVolume(volume);
                    break;
                case VolumeType.SFX:
                    _volumeSystem.SetSoundFxVolume(volume);
                    break;
            }
        }
    }
}