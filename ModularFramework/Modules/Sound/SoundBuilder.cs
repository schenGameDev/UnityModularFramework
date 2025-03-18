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
            if(soundManager.soundFxs.ContainsKey(profileName)) PlaySound(profileName);
            else PlayMusic(profileName);
        }

        public void PlaySound(string profileName) {
            if (profileName == null) {
                DebugUtil.Error("Sound is null");
                return;
            }

            SoundProfile profile = soundManager.soundFxs.Get(profileName).OrElseThrow(new KeyNotFoundException(profileName));
            profile.isBGM = false;

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

        public void PlayMusic(string profileName) {
            if (profileName == null) {
                DebugUtil.Error("Sound is null");
                return;
            }

            SoundProfile profile = soundManager.tracks.Get(profileName).OrElseThrow(new KeyNotFoundException(profileName));
            profile.isBGM = true;
            profile.loop = soundManager.repeat; // For playlist functionality, we want tracks to play once
            profile.bypassListenerEffects = true;

            SoundPlayer soundPlayer = soundManager.MusicPlayers[0];
            soundPlayer.Initialize(profile, soundManager.tracks.mixerGroup,soundManager);
            soundPlayer.SetVolume(0);

            soundPlayer.Play();
        }
    }