using ModularFramework;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SoundProfileBucket_SO", menuName = "Game Module/Sound/Sound Profile Bucket")]
public class SoundProfileBucket : SOBucket<SoundProfile> {
    public AudioMixerGroup mixerGroup;
}