using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;
using UnityEngine.Audio;
using Void = EditorAttributes.Void;

/// <summary>
/// Manage BGM in game, use one system throughout the game
/// </summary>
[CreateAssetMenu(fileName = "MusicSystem_SO", menuName = "Game Module/Sound/Music System")]
public class MusicSystemSO : GameSystem
{
    [Header("Config")]
    [SerializeField] public AudioMixerGroup masterGroup,soundFxGroup;
    public SoundProfileBucket tracks;

    [Header("Music")]
    [SerializeField] private float crossFadeTime = 1.0f;
    [SerializeField] public bool repeat = true;
    [SerializeField, ShowField(nameof(repeat)), Suffix("repeat the first track")] public bool repeatOnAwake = true;
    [SerializeField] List<string> defaultPlaylist = new();
    
    [FoldoutGroup("Event Channels", nameof(trackChannel))]
    [SerializeField] private Void eventChannelGroup;
    [HideInInspector,SerializeField] StringEventChannelSO trackChannel;
    
    [Header("Runtime")]
    [ReadOnly,SerializeField,RuntimeObject,Rename("Current Track")] string currentTrackName;
    public readonly AudioSource[] MusicPlayers = new AudioSource[2];
    //public int currentActiveMusicPlayer;
    [RuntimeObject] public SoundProfile CurrentTrack {get; set;}
    [RuntimeObject] public SoundProfile PrevTrack {get; set;}
    [SerializeField, ReadOnly,RuntimeObject] List<string> playlist = new();
    [RuntimeObject] Transform PersistentSoundParent {get;set;}
    
    private void OnEnable()
    {
        trackChannel?.AddListener(PlayTrack);
    }
    
    private void OnDisable()
    {
        trackChannel?.RemoveListener(PlayTrack);
    }


    public override void OnStart()
    {
        BuildPersistentParent();
        InitializeMusic();
    }
    public override void OnDestroy()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // nothing
        }
    }

    void BuildPersistentParent() {
        PersistentSoundParent = new GameObject("=== Sounds (Permisstent) ===").transform;
        PersistentSoundParent.position = Vector3.zero;
        //DontDestroyOnLoad(PersistentSoundParent.gameObject);

    }

#region Looping Music
    bool PlayMusic => tracks;
    void InitializeMusic() {
        if(!PlayMusic) return;
        CurrentTrack = null;
        PrevTrack = null;
        playlist.Clear();

        MusicPlayers[0] = new GameObject("MusicPlayerA").AddComponent<AudioSource>();
        MusicPlayers[0].transform.parent = PersistentSoundParent;
        MusicPlayers[0].outputAudioMixerGroup = tracks.mixerGroup;
        //MusicPlayers[0].gameObject.SetActive(false);

        MusicPlayers[1] = new GameObject("MusicPlayerB").AddComponent<AudioSource>();
        MusicPlayers[1].transform.parent = PersistentSoundParent;
        MusicPlayers[1].outputAudioMixerGroup = tracks.mixerGroup;
        //MusicPlayers[1].gameObject.SetActive(false);

        if(defaultPlaylist==null) return;
        if(!repeat) {
            foreach (var p in defaultPlaylist) {
                AddToPlaylist(p);
            }
        } else if(repeatOnAwake) {
            PlayTrack(defaultPlaylist[0]);
        }
    }

    public void ShufflePlaylist() {
        playlist.Shuffle();
        defaultPlaylist.Shuffle();
    }

    public void PlayTrackNext(string trackName) {
        playlist.Remove(trackName);
        playlist.Insert(0, trackName);
    }

    private void AddToPlaylist(string profileName) {
        playlist.Add(profileName);
        if (!CurrentTrack && !PrevTrack) {
            PlayNextTrack();
        }
    }

    [RuntimeObject] private CancellationTokenSource _cts;
    public void PlayTrack(string trackName) {
        if(CurrentTrack && CurrentTrack.name == trackName) return;
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        if(CurrentTrack) {
            PrevTrack = CurrentTrack;
            (MusicPlayers[0], MusicPlayers[1]) = (MusicPlayers[1], MusicPlayers[0]);
        }

        CurrentTrack = tracks.Get(trackName).Get();

        Play(trackName);
        currentTrackName = trackName;
        CrossFade( crossFadeTime, _cts.Token).Forget();
    }

    public void PlayNextTrack() {
        if (playlist.NonEmpty()) {
            PlayTrack(playlist.RemoveAtAndReturn(0));
        } else if(defaultPlaylist!=null) {
            foreach (var p in defaultPlaylist) {
                AddToPlaylist(p);
            }
        }
    }

    // [RuntimeObject] float _fading;// starting volume?
    // void CrossFade(float deltaTime) {
    //     if(_fading <= 0f) return;
    //
    //     _fading += deltaTime;
    //
    //     float fraction = Mathf.Clamp01(_fading / crossFadeTime);
    //
    //     // Logarithmic fade
    //     float logFraction = Mathf.Log10(1 + 9 * fraction) / Mathf.Log10(10);
    //
    //     if (PrevTrack && musicPlayers[1].Profile) musicPlayers[1].SetVolume(Mathf.Min(musicPlayers[1].Profile.volume, 1 - logFraction));
    //     if (CurrentTrack && musicPlayers[0].Profile) musicPlayers[0].SetVolume(Mathf.Max(musicPlayers[0].Profile.volume, logFraction));
    //
    //     if (fraction >= 1) {
    //         _fading = 0.0f;
    //         if (PrevTrack) {
    //             musicPlayers[1].Stop();
    //             PrevTrack = null;
    //         }
    //     }
    // }
    
    void Play(string profileName) {
        if (profileName == null) {
            DebugUtil.Error("Sound is null");
            return;
        }

        SoundProfile profile = tracks.Get(profileName).OrElseThrow(new KeyNotFoundException(profileName));
        AudioSource soundPlayer = MusicPlayers[0];
        
        soundPlayer.clip = profile.clip;
        soundPlayer.loop = repeat; // For playlist functionality, we want tracks to play once
        soundPlayer.bypassListenerEffects = profile.bypassListenerEffects;
        soundPlayer.volume = 0;

        soundPlayer.Play();
        MonitorTrackProgress(soundPlayer);
    }
    
    private void TrackComplete()
    {
        if(!PlayMusic) return;
        if(!repeat) PlayNextTrack();
    }
    
    async UniTaskVoid CrossFade(float duration, CancellationToken token)
    {
        float fading = 0;
        float fraction = 0;
        while (fraction < 1)
        {
            // Logarithmic fade
            float logFraction = Mathf.Log10(1 + 9 * fraction) / Mathf.Log10(10);

            if (PrevTrack && MusicPlayers[1]) MusicPlayers[1].volume = Mathf.Min(MusicPlayers[1].volume, 1 - logFraction);
            if (CurrentTrack && MusicPlayers[0]) MusicPlayers[0].volume = Mathf.Max(MusicPlayers[0].volume, logFraction);
            
            fading += Time.deltaTime;
            fraction = Mathf.Clamp01(fading / duration);

            await UniTask.NextFrame(cancellationToken: token);
        }
        if (PrevTrack) {
            MusicPlayers[1].Stop();
            PrevTrack = null;
        }
    }
    #endregion

    #region Monitor
    [RuntimeObject] CancellationTokenSource _cts2;
    public void MonitorTrackProgress(AudioSource audioSource) {
        if(_cts2!=null) {
            _cts2.Cancel();
            _cts2.Dispose();
        }
        _cts2 = new CancellationTokenSource();

        WaitForSoundToEnd(audioSource,_cts.Token).Forget();

    }

    async UniTaskVoid WaitForSoundToEnd(AudioSource audio,CancellationToken token)
    {
        await UniTask.WaitWhile(()=>audio && audio.isPlaying, cancellationToken: token).SuppressCancellationThrow();
        TrackComplete();
    }

    public void Stop() {
        _cts.Cancel();
        MusicPlayers[0].Stop();
    }

    #endregion 
#region Volume
    public void SetVolume(AudioMixerGroup mixerGroup, float volume) {
        mixerGroup.audioMixer.SetFloat(mixerGroup.name, Mathf.Log10(volume) * 20f);
    }

    public void SetMasterVolume(float volume) => SetVolume(masterGroup, volume);
    public void SetMusicVolume(float volume) => SetVolume(tracks.mixerGroup, volume);
    public void SetSoundFxVolume(float volume) => SetVolume(soundFxGroup, volume);
    
    public float GetVolume(AudioMixerGroup mixerGroup)
    {
        if (mixerGroup.audioMixer.GetFloat(mixerGroup.name, out var volume))
        {
            return Mathf.Clamp(Mathf.Pow(10, volume / 20f), 0, 1);
        }
        return 1;
    }
    
    public float GetMasterVolume() => GetVolume(masterGroup);
    public float GetMusicVolume() => GetVolume(tracks.mixerGroup);
    public float GetSoundFxVolume() => GetVolume(soundFxGroup);
#endregion
#region Editor
    [Button]
    void PopulateDefaultPlaylist() {
        if(tracks != null) {
            defaultPlaylist.Clear();
            tracks.ForEach(track => defaultPlaylist.Add(track.name));
        }
    }
    
#endregion
}