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
        private Autowire<VolumeSystemSO> _volumeSystem = new();

        private void Start()
        {
            _slider = GetComponent<Slider>();
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
                    _slider.value = _volumeSystem.Get().GetMasterVolume();
                    break;
                case VolumeType.MUSIC:
                    _slider.value = _volumeSystem.Get().GetMusicVolume();
                    break;
                case VolumeType.SFX:
                    _slider.value = _volumeSystem.Get().GetSoundFxVolume();
                    break;
            }
        }

        public void SetVolume(float volume)
        {
            switch (volumeType)
            {
                case VolumeType.MASTER:
                    _volumeSystem.Get().SetMasterVolume(volume);
                    break;
                case VolumeType.MUSIC:
                    _volumeSystem.Get().SetMusicVolume(volume);
                    break;
                case VolumeType.SFX:
                    _volumeSystem.Get().SetSoundFxVolume(volume);
                    break;
            }
        }
    }
}