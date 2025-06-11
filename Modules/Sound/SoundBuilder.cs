using System.Collections.Generic;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

public class SoundBuilder {
        readonly SoundManagerSO soundManager;
        Vector3 position = Vector3.zero;

        public SoundBuilder() {
            this.soundManager = GameRunner.Instance?.GetModule<SoundManagerSO>().OrElse(null);
        }

        public SoundBuilder WithPosition(Vector3 position) {
            this.position = position;
            return this;
        }

        public SoundPlayer Play(string profileName, bool loop = false) {
            if (profileName == null) {
                DebugUtil.Error("Sound is null");
                return null;
            }

            SoundProfile profile = soundManager.GetSound(profileName);

            if (!soundManager.CanPlaySound(profile)) return null;

            SoundPlayer soundPlayer = soundManager.GetPlayer();
            soundPlayer.Initialize(profile);
            soundPlayer.transform.position = position;
            soundPlayer.transform.parent = soundManager.SoundParent;
            
            if (profile.frequentSound) {
                soundPlayer.Node = soundManager.FrequentSoundPlayers.AddLast(soundPlayer);
            }

            soundPlayer.Play();
            return soundPlayer;
        }
    
    }