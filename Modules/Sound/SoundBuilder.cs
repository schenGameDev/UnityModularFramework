using System.Collections.Generic;
using ModularFramework.Utility;
using UnityEngine;

public class SoundBuilder {
        readonly SoundManagerSO soundManager;
        Vector3 position = Vector3.zero;

        public SoundBuilder(SoundManagerSO soundManager) {
            this.soundManager = soundManager;
        }

        public SoundBuilder WithPosition(Vector3 position) {
            this.position = position;
            return this;
        }

        public void Play(string profileName) {
            if (profileName == null) {
                DebugUtil.Error("Sound is null");
                return;
            }
            PlaySound(profileName);
        }

        public void PlaySound(string profileName) {
            if (profileName == null) {
                DebugUtil.Error("Sound is null");
                return;
            }

            SoundProfile profile = soundManager.soundFxs.Get(profileName).OrElseThrow(new KeyNotFoundException(profileName));

            if (!soundManager.CanPlaySound(profile)) return;

            SoundPlayer soundPlayer = soundManager.Get();
            soundPlayer.Initialize(profile, soundManager.soundFxs.mixerGroup,soundManager);
            soundPlayer.transform.position = position;
            soundPlayer.transform.parent = soundManager.SoundParent;


            if (profile.frequentSound) {
                soundPlayer.Node = soundManager.FrequentSoundPlayers.AddLast(soundPlayer);
            }

            soundPlayer.Play();
        }

        
    }