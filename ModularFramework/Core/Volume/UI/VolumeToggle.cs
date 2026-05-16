using KBCore.Refs;
using ModularFramework;
using UnityEngine;
using UnityEngine.UI;
using VolumeType = UnityModularFramework.VolumeSystemSO.VolumeType;

namespace UnityModularFramework
{
    public class VolumeToggle : MonoBehaviour
    {
        [SerializeField] private VolumeType volumeType;
        [SerializeField] private Image volumeIcon;
        [SerializeField] private Image silenceIcon;
        
        [SerializeField,Self] private Button silenceButton;
        
        private Autowire<VolumeSystemSO> _volumeSystem = new();
        private float _lastVolume;
        private bool _volumeOn;
        
        private void Start()
        {
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
                    _lastVolume = _volumeSystem.Get().GetMasterVolume();
                    break;
                case VolumeType.MUSIC:
                    _lastVolume = _volumeSystem.Get().GetMusicVolume();
                    break;
                case VolumeType.SFX:
                    _lastVolume = _volumeSystem.Get().GetSoundFxVolume();
                    break;
            }
            _volumeOn = _lastVolume > 0;
            SetImage(_volumeOn);
        }

        public void ToggleVolume()
        {
            _volumeOn = !_volumeOn;
            var vcs = _volumeSystem.Get();
            var volume = _volumeOn ? _lastVolume : 0;
            switch (volumeType)
            {
                case VolumeType.MASTER:
                    if (!_volumeOn) _lastVolume = vcs.GetMasterVolume();
                    vcs.SetMasterVolume(volume);
                    break;
                case VolumeType.MUSIC:
                    if (!_volumeOn) _lastVolume = vcs.GetMusicVolume();
                    _volumeSystem.Get().SetMusicVolume(volume);
                    break;
                case VolumeType.SFX:
                    if (!_volumeOn) _lastVolume = vcs.GetSoundFxVolume();
                    _volumeSystem.Get().SetSoundFxVolume(volume);
                    break;
            }
            SetImage(_volumeOn);
        }

        private void SetImage(bool on)
        {
            volumeIcon.enabled = on;
            silenceIcon.enabled = !on;
        }
        
#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif
    }
}