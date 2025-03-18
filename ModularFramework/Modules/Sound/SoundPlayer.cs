using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ModularFramework;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour {
    AudioSource _audio;
    float _delay;
    SoundManagerSO _soundManager;
    CancellationTokenSource _cts;
    public SoundProfile Profile {get; private set;}
    public LinkedListNode<SoundPlayer> Node { get; set; }

    void Awake()
    {
        _audio = gameObject.GetOrAdd<AudioSource>();
    }

    public void Initialize(SoundProfile soundProfile, AudioMixerGroup mixerGroup, SoundManagerSO soundManager)
    {
        _soundManager = soundManager;
        Profile = soundProfile;
        _audio.clip = soundProfile.clip;
        _audio.outputAudioMixerGroup = mixerGroup;
        _audio.loop = soundProfile.loop;

        _audio.volume = soundProfile.volume;
        _audio.pitch = soundProfile.pitch;
        _audio.bypassListenerEffects = soundProfile.bypassListenerEffects;

        _delay = soundProfile.delay;
    }

    public void Play() {
        if(_cts!=null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        if(_delay<=0) _audio.Play();
        else _audio.PlayDelayed(_delay);

        WaitForSoundToEnd(_cts.Token, _delay).Forget();

    }

    async UniTaskVoid WaitForSoundToEnd(CancellationToken token, float delay) {
        if(delay>0) await UniTask.WaitForSeconds(delay + 0.001f, cancellationToken: token);
        await UniTask.WaitWhile(()=>_audio!=null && _audio.isPlaying, cancellationToken: token);
        Stop();
    }

    public void Stop() {
        if(_cts!=null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        if(_audio==null) return;
        _audio.Stop();
        if(!Profile.isBGM) _soundManager.ReturnToPool(this);
        else _soundManager.PlayNextTrack();
    }

    public void SetVolume(float volume) => _audio.volume = volume;

    public bool IsPlaying() => _audio.isPlaying;
}