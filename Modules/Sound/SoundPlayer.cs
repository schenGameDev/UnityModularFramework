using System;
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
    private float _vol;
    SoundManagerSO _soundManager;
    CancellationTokenSource _cts;
    public SoundProfile Profile {get; private set;}
    public LinkedListNode<SoundPlayer> Node { get; set; }

    void Awake()
    {
        _audio = gameObject.GetOrAdd<AudioSource>();
    }

    public void Initialize(SoundProfile soundProfile, AudioMixerGroup mixerGroup, SoundManagerSO soundManager, bool loop = false)
    {
        _soundManager = soundManager;
        Profile = soundProfile;
        _audio.clip = soundProfile.clip;
        _audio.outputAudioMixerGroup = mixerGroup;

        _audio.volume = soundProfile.volume;
        _vol = soundProfile.volume;
        _audio.pitch = soundProfile.pitch;
        _audio.bypassListenerEffects = soundProfile.bypassListenerEffects;
        _audio.loop = loop;

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

    async UniTaskVoid WaitForSoundToEnd(CancellationToken token, float delay)
    {
        bool isCancelled = false;
        if(delay>0) isCancelled= await UniTask.WaitForSeconds(delay + 0.001f, cancellationToken: token).SuppressCancellationThrow();
        if(!isCancelled)
            await UniTask.WaitWhile(()=>_audio && _audio.isPlaying, cancellationToken: token).SuppressCancellationThrow();
        _soundManager.ReturnToPool(this);
    }

    public void Stop() {
        if(_cts!=null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        if(!_audio) return;
        _audio.Stop();
        _soundManager.ReturnToPool(this);
    }
    
    public void SetVolume(float volume) => _audio.volume = volume;
    public void ResetVolume() => _audio.volume = _vol;

    private void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}