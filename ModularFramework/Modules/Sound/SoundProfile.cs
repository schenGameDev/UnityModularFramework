using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SoundProfile_SO", menuName = "Game Module/Sound/Sound Profile")]
public class SoundProfile : ScriptableObject {
    public AudioClip clip;
    //public bool loop;
    //public bool playOnAwake = true;
    public float delay;
    public bool frequentSound;


    // public bool mute;
    // public bool bypassEffects;
    public bool bypassListenerEffects; // BGM true
    // public bool bypassReverbZones;

    // public int priority = 128;
    public float volume = 1f;
    public float pitch = 1f;
    // public float panStereo;
    // public float spatialBlend;
    // public float reverbZoneMix = 1f;
    // public float dopplerLevel = 1f;
    // public float spread;

    // public float minDistance = 1f;
    // public float maxDistance = 500f;

    // public bool ignoreListenerVolume;
    // public bool ignoreListenerPause;

    // public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
}
