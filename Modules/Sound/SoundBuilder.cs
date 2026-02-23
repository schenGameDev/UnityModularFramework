using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework.Modules.Sound
{
    public class SoundBuilder
    {
        private readonly Autowire<SoundManagerSO> _soundManager = new();
        private Vector3 _position = Vector3.zero;

        public SoundBuilder WithPosition(Vector3 position)
        {
            this._position = position;
            return this;
        }

        public SoundPlayer Play(string profileName, bool loop = false)
        {
            if (profileName == null)
            {
                DebugUtil.Error("Sound is null");
                return null;
            }

            SoundProfile profile = _soundManager.Get().GetSound(profileName);

            if (!_soundManager.Get().CanPlaySound(profile)) return null;

            SoundPlayer soundPlayer = _soundManager.Get().GetPlayer();
            soundPlayer.Initialize(profile);
            soundPlayer.transform.position = _position;
            soundPlayer.transform.parent = _soundManager.Get().SoundParent;

            if (profile.frequentSound)
            {
                soundPlayer.Node = _soundManager.Get().FrequentSoundPlayers.AddLast(soundPlayer);
            }

            soundPlayer.Play();
            return soundPlayer;
        }

    }
}