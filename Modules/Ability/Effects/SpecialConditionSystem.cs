using UnityEngine;
using UnityTimer;

namespace ModularFramework.Modules.Ability
{
    public class SpecialConditionSystem : GameSystem<SpecialConditionSystem>
    {
        [Header("Bleed")]
        [SerializeField, Min(0)] private float bleedEveryNSeconds = 2;
        [SerializeField, Min(0)] private float bleedDamage = 2;

        public LimitedRepeatTimer GetTimer(IDamageable target, SpecialCondition specialCondition, float duration)
        {
            var timer = new LimitedRepeatTimer(bleedEveryNSeconds, Mathf.CeilToInt(duration / bleedEveryNSeconds));
            timer.OnTick = () =>
            {
                if (specialCondition == SpecialCondition.Bleed)
                {
                    target.TakeDamage(bleedDamage, DamageType.Physical, null);
                }
            };
            timer.OnTimerStop = () => target.EffectResolver.RemoveSpecialCondition(specialCondition);
            return timer;
        }
        
        protected override void OnAwake()
        {
        }

        protected override void OnStart()
        {
        }

        protected override void OnSceneDestroy()
        {
        }
    }
}