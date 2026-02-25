using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using Void = EditorAttributes.Void;

namespace ModularFramework.Modules.Ability
{
    public abstract class AbilitySO : ScriptableObject
    {
        [Title(nameof(description), 12, stringInputMode: StringInputMode.Dynamic), SerializeField]
        private Void titleHolder;

        protected string description;

        public AudioClip releaseSfx;
        public GameObject[] releaseVfx;

        [Min(0)] public float cooldown;
        [HideInInspector] public Vector3 emitOffset; // set externally

        public abstract AimType AimMethod();
        public abstract float AimRange();
        
        [SerializeField, Tooltip("If true, i will stay in this state until end or interrupted")]
        public bool continuousCasting;

        public void Release(Transform me, List<IDamageable> targets, Action<AbilitySO> onComplete)
        {
            PlayVisualSoundEffects(me, targets, null, null);
            Apply(me, targets, continuousCasting && onComplete!=null? () => onComplete(this) : null);
            if (!continuousCasting && onComplete != null)
            {
                onComplete(this);
            }
        }
        
        public virtual void ReleaseDirection(Transform me, Vector3 direction, Action<AbilitySO> onComplete)
        {
            onComplete?.Invoke(this);
        }

        public virtual void ReleasePosition(Transform me, Vector3 position, Action<AbilitySO> onComplete)
        {
            onComplete?.Invoke(this);
        }

        protected abstract void Apply(Transform me, List<IDamageable> targets, Action onComplete);

        protected virtual void PlayVisualSoundEffects(Transform me, List<IDamageable> targets,
            List<Vector3> tarPositions, List<Vector3> directions)
        {
        }
    }
}