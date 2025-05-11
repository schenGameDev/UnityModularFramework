using ModularFramework;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeController : MonoBehaviour
{
    public enum VolumeType {MASTER, MUSIC, SFX}
    
    [SerializeField] private VolumeType volumeType;
    
    private Slider _slider;
    private MusicSystemSO _musicSystem;
    private void Start()
    {
        _slider = GetComponent<Slider>();
        _musicSystem = GameRunner.GetSystem<MusicSystemSO>().Get();
        InitializeVolume();
    }

    private void OnEnable()
    {
        if(!didStart) return;
        InitializeVolume();
    }

    private void InitializeVolume()
    {
        switch (volumeType)
        {
            case VolumeType.MASTER:
                _slider.value =  _musicSystem.GetMasterVolume();
                break;
            case VolumeType.MUSIC:
                _slider.value =  _musicSystem.GetMusicVolume();
                break;
            case VolumeType.SFX:
                _slider.value =  _musicSystem.GetSoundFxVolume();
                break;
        }
    }

    public void SetVolume(float volume)
    {
        switch (volumeType)
        {
            case VolumeType.MASTER:
                _musicSystem.SetMasterVolume(volume);
                break;
            case VolumeType.MUSIC:
                _musicSystem.SetMusicVolume(volume);
                break;
            case VolumeType.SFX:
                _musicSystem.SetSoundFxVolume(volume);
                break;
        }
    }
}