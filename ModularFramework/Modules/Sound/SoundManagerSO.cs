using UnityEngine;
using ModularFramework;
using UnityEngine.Audio;
using EditorAttributes;
using UnityEngine.Pool;
using System.Collections.Generic;
using ModularFramework.Utility;
/// <summary>
/// Manage all one-shot sound in game. <br/>
/// 1. Must bootup before any module that calls it.<br/>
/// 2. Must use the same soundManager throughout the game.
/// </summary>
[CreateAssetMenu(fileName = "SoundManager_SO", menuName = "Game Module/Sound/Sound Manager")]
public class SoundManagerSO : GameModule
{
    const float crossFadeTime = 1.0f;

    [Header("Config")]
    [SerializeField] public AudioMixerGroup _masterGroup;
    [SerializeField] SoundPlayer _soundPlayerPrefab;
    [Header("Sound")]
    public SoundProfileBucket soundFxs;
    [SerializeField] bool _collectionCheck = true;
    [SerializeField] int _defaultCapacity = 5;
    [SerializeField] int _maxPoolSize = 100;
    [SerializeField] int _maxSoundInstances = 15;

    [Header("Music")]
    [OnValueChanged(nameof(ChangeUpdateMode))] public SoundProfileBucket tracks;
    [SerializeField] public bool repeat = true;
    [SerializeField, ShowField(nameof(repeat)), Suffix("repeat the first track")] public bool repeatOnAwake = true;
    [SerializeField] List<string> _defaultPlaylist = new();


    [Header("Runtime")]
    [ReadOnly,SerializeField] string _currentTrack;
    [HideInInspector] public SoundPlayer[] MusicPlayers = new SoundPlayer[2];
    //public int currentActiveMusicPlayer;
    public SoundProfile CurrentTrack {get; set;}
    public SoundProfile PrevTrack {get; set;}
    [SerializeField, ReadOnly] List<string> _playlist = new();
    public Transform SoundParent {get; private set;}
    public Transform PersistentSoundParent {get; private set;}

    [RuntimeObject] readonly List<SoundPlayer> _activeSoundPlayers = new();
    [HideInInspector,RuntimeObject(nameof(ResetFrequentSoundPlayer),nameof(ResetFrequentSoundPlayer))]
    public readonly LinkedList<SoundPlayer> FrequentSoundPlayers = new();

    bool _isStartedAlready => SoundParent;


    public SoundManagerSO() {
        updateMode = UpdateMode.NONE;
    }

    void ChangeUpdateMode() {
        if(_playMusic) {
            updateMode = UpdateMode.EVERY_N_FRAME;
        } else updateMode = UpdateMode.NONE;
    }

    public override void OnStart()
    {
        base.OnStart();
        if(!_isStartedAlready) {
            BuildParent();
            BuildPersistentParent();
            InitializeMusic();
        }

        InitializePool();
    }

    public override void OnUpdate(float deltaTime)
    {
        base.OnUpdate(deltaTime);
        if(!_playMusic) return;
        CrossFade(deltaTime);
        if(!repeat && !MusicPlayers[0].IsPlaying() && _playlist.Count > 0) {
            PlayNextTrack();
        }
    }

    public SoundBuilder CreateSoundBuilder() => new SoundBuilder(this);

    void BuildParent() {
        SoundParent = new GameObject("=== Sounds ===").transform;
        SoundParent.position = Vector3.zero;
    }

    void BuildPersistentParent() {
        PersistentSoundParent = new GameObject("=== Sounds (Permisstent) ===").transform;
        PersistentSoundParent.position = Vector3.zero;
        DontDestroyOnLoad(PersistentSoundParent.gameObject);

    }

#region Looping Music
    bool _playMusic => tracks;
    void InitializeMusic() {
        if(!_playMusic) return;
        CurrentTrack = null;
        PrevTrack = null;
        _playlist.Clear();

        MusicPlayers[0] = Instantiate(_soundPlayerPrefab);
        MusicPlayers[0].transform.parent = PersistentSoundParent;
        //MusicPlayers[0].gameObject.SetActive(false);
        MusicPlayers[0].name = "MusicPlayerA";

        MusicPlayers[1] = Instantiate(_soundPlayerPrefab);
        MusicPlayers[1].transform.parent = PersistentSoundParent;
        //MusicPlayers[1].gameObject.SetActive(false);
        MusicPlayers[1].name = "MusicPlayerB";

        if(_defaultPlaylist==null) return;
        if(!repeat) {
            foreach (var p in _defaultPlaylist) {
                AddToPlaylist(p);
            }
        } else if(repeatOnAwake) {
            PlayTrack(_defaultPlaylist[0]);
        }
    }

    public void ShufflePlaylist() {
        _playlist.Shuffle();
        _defaultPlaylist.Shuffle();
    }

    public void PlayTrackNext(string trackName) {
        _playlist.Remove(trackName);
        _playlist.Insert(0, trackName);
    }

    public void AddToPlaylist(string profileName) {
        _playlist.Add(profileName);
        if (CurrentTrack == null && PrevTrack == null) {
            PlayNextTrack();
        }
    }

    public void PlayTrack(string trackName) {
        if(CurrentTrack) {
            if (CurrentTrack.name == trackName) return;

            PrevTrack = CurrentTrack;
            var temp = MusicPlayers[0];
            MusicPlayers[0] = MusicPlayers[1];
            MusicPlayers[1] = temp;
        }

        CurrentTrack = tracks.Get(trackName).Get();

        CreateSoundBuilder().PlayMusic(trackName);
        _currentTrack = trackName;
        _fading = 0.001f;
    }

    public void PlayNextTrack() {
        if (_playlist.NonEmpty()) {
            PlayTrack(_playlist.RemoveAtAndReturn(0));
        } else if(_defaultPlaylist!=null) {
            foreach (var p in _defaultPlaylist) {
                AddToPlaylist(p);
            }
        }
    }

    [RuntimeObject] float _fading;// starting volume?
    void CrossFade(float deltaTime) {
        if(_fading <= 0f) return;

        _fading += deltaTime;

        float fraction = Mathf.Clamp01(_fading / crossFadeTime);

        // Logarithmic fade
        float logFraction = Mathf.Log10(1 + 9 * fraction) / Mathf.Log10(10);

        if (PrevTrack && MusicPlayers[1].Profile) MusicPlayers[1].SetVolume(Mathf.Min(MusicPlayers[1].Profile.volume, 1 - logFraction));
        if (CurrentTrack && MusicPlayers[0].Profile) MusicPlayers[0].SetVolume(Mathf.Max(MusicPlayers[0].Profile.volume, logFraction));

        if (fraction >= 1) {
            _fading = 0.0f;
            if (PrevTrack) {
                MusicPlayers[1].Stop();
                PrevTrack = null;
            }
        }
    }

    #endregion

    #region Play SoundFx
    public bool CanPlaySound(SoundProfile data) {
        if (!data.frequentSound) return true;

        if (FrequentSoundPlayers.Count >= _maxSoundInstances) {
            try {
                FrequentSoundPlayers.First.Value.Stop();
                return true;
            } catch {
                DebugUtil.DebugLog("SoundEmitter is already released");
            }
            return false;
        }
        return true;
    }

    public SoundPlayer Get() => _soundPlayerPool.Get();
    public void ReturnToPool(SoundPlayer soundPlayer) {
        if (soundPlayer.Node != null) {
            FrequentSoundPlayers.Remove(soundPlayer.Node);
            soundPlayer.Node = null;
        }
        _soundPlayerPool.Release(soundPlayer);
    }
    public void StopAll() {
        _activeSoundPlayers.ForEach(x=>x.Stop());
    }
    [RuntimeObject] ObjectPool<SoundPlayer> _soundPlayerPool;
    void InitializePool() {
        _soundPlayerPool = new ObjectPool<SoundPlayer>(
            () => {
                var soundPlayer = Instantiate(_soundPlayerPrefab);
                soundPlayer.gameObject.SetActive(false);
                return soundPlayer;
            },
            sp => {
                sp.gameObject.SetActive(true);
                _activeSoundPlayers.Add(sp);
            },
            sp => {
                sp.gameObject.SetActive(false);
                _activeSoundPlayers.Remove(sp);
            },
            sp => {
                _activeSoundPlayers.Remove(sp);
                if (sp.Node != null) {
                    FrequentSoundPlayers.Remove(sp.Node);
                }
                Destroy(sp.gameObject);
            },
            _collectionCheck,
            _defaultCapacity,
            _maxPoolSize);
    }

    public void ResetFrequentSoundPlayer() => FrequentSoundPlayers.Clear();
#endregion

#region Volume
    public void SetVolume(AudioMixerGroup mixerGroup, float volume) {
        mixerGroup.audioMixer.SetFloat(mixerGroup.name, Mathf.Log10(volume) * 20f);
    }

    public void SetMasterVolume(float volume) => SetVolume(_masterGroup, volume);
    public void SetMusicVolume(float volume) => SetVolume(tracks.mixerGroup, volume);
    public void SetSoundFxVolume(float volume) => SetVolume(soundFxs.mixerGroup, volume);
#endregion
#region Editor
    [Button]
    void PopulateDefaultPlaylist() {
        if(tracks != null) {
            _defaultPlaylist.Clear();
            tracks.ForEach(track => _defaultPlaylist.Add(track.name));
        }
    }
#endregion
}
