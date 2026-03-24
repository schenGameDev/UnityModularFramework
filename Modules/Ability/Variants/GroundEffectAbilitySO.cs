using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    [CreateAssetMenu(fileName = "Ground Effect Ability", menuName = "Game Module/Ability/Ground Effect Ability")]
    public class GroundEffectAbilitySO : AbilitySO
    {
        public GroundEffectAbilitySO()
        {
            description =
                "Instantly create a ground effect at position, targets are evaluated after ground effect is spawned\n\n" +
                "Can be used for poison pool, aoe attack etc.";
        }

        [SerializeField] private ImpactEffect impactEffectPrefab;

        [SerializeReference, SubclassSelector]
        [Tooltip(
            "Used when spawn position is not the targets, but a different position calculated from the targets (e.g. center of crowd)")]
        private IPositionCalculator positionCalculator;

        [SerializeField] bool matchCasterRotation = true;
        [SerializeField] private bool targetSelf;
        [SerializeField,HideField(nameof(targetSelf))] private float maxRange = -1;
        
        public override AimType AimMethod() => targetSelf ? AimType.Self : AimType.Position;
        public override float AimRange() => targetSelf? 0 : maxRange;
        
        protected override void Apply(Transform me, List<IDamageable> targets, Action onComplete)
        {
            Vector3 rotatedOffset = me.rotation * emitOffset;
            Quaternion rotatedRotation = matchCasterRotation ? me.rotation : Quaternion.identity;

            if (targetSelf)
            {
                var groundEffect = Instantiate(impactEffectPrefab, me.position + rotatedOffset, rotatedRotation);
                groundEffect.onComplete = onComplete;
            }
            else if (positionCalculator != null)
            {
                Vector3 spawnPosition =
                    positionCalculator.GetPosition(me.position, targets?.Select(t => t.Transform.position));
                var groundEffect = Instantiate(impactEffectPrefab, spawnPosition + rotatedOffset, rotatedRotation);
                groundEffect.onComplete = onComplete;
            }
            else
            {
                foreach (var target in targets)
                {
                    var groundEffect = Instantiate(impactEffectPrefab, target.Transform.position + rotatedOffset,
                        rotatedRotation);
                    groundEffect.onComplete = onComplete;
                }
            }
        }

        public override void ReleasePosition(Transform me, Vector3 targetPos, Action<AbilitySO> onComplete)
        {
            PlayVisualSoundEffects(me, null,new List<Vector3>() {targetPos}, null);
            if (!continuousCasting)
            {
                onComplete?.Invoke(this);
                onComplete = null;
            }

            Vector3 rotatedOffset = me.rotation * emitOffset;
            Quaternion rotatedRotation = matchCasterRotation ? me.rotation : Quaternion.identity;

            if (targetSelf)
            {
                var groundEffect = Instantiate(impactEffectPrefab, me.position + rotatedOffset, rotatedRotation);
                groundEffect.onComplete = () => onComplete?.Invoke(this);
            }
            else if (positionCalculator != null)
            {
                Vector3 spawnPosition = positionCalculator.GetPosition(me.position, new List<Vector3>() { targetPos });
                var groundEffect = Instantiate(impactEffectPrefab, spawnPosition + rotatedOffset, rotatedRotation);
                groundEffect.onComplete = () => onComplete?.Invoke(this);
            }
            else
            {
                var groundEffect = Instantiate(impactEffectPrefab, targetPos + rotatedOffset, rotatedRotation);
                groundEffect.onComplete = () => onComplete?.Invoke(this);
            }
        }
    }
}