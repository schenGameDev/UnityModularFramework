using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework.Modules.Targeting;
using UnityEngine;
using UnityTimer;

namespace ModularFramework.Modules.Ability
{
    [DisallowMultipleComponent]
    public class ImpactEffect : MonoBehaviour
    {
        [SerializeField] private bool impactOverTime = false;

        [HideField(nameof(impactOverTime)), Min(0), SerializeField]
        private float waitBeforeDestroy;

        [ShowField(nameof(impactOverTime)), SerializeField, Tooltip("Apply effects to target in range every n seconds"),
         Min(0), Suffix("s")]
        private float tickInterval;

        [ShowField(nameof(impactOverTime)), SerializeField, Min(0)]
        private int ticks;

        [SerializeField] RangeFilter rangeFilter;
        [SerializeReference, SubclassSelector] public List<IEffectFactory<IDamageable>> effects = new();
        public Action onComplete;

        private Timer _timer;
        private HashSet<DamageTarget> _affectedTargets; // singular types

        //public override void OnStartServer()
        private void Start()
        {
            _affectedTargets = GetAllTargetTypes();
            if (impactOverTime)
            {
                _timer = new LimitedRepeatTimer(tickInterval, ticks);
                _timer.OnTick = ApplyEffects;
                _timer.OnTimerStop = OnStop;
                _timer.Start();
            }
            else
            {
                ApplyEffects();
                if (waitBeforeDestroy > 0)
                {
                    _timer = new CountdownTimer(waitBeforeDestroy);
                    _timer.OnTimerStop = OnStop;
                    _timer.Start();
                }
                else
                {
                    // NetworkServer.
                    Destroy(gameObject);
                }
            }
        }

        private HashSet<DamageTarget> GetAllTargetTypes()
        {
            var types = new HashSet<DamageTarget>();
            foreach (var effectFactory in effects)
            {
                var effect = effectFactory.Create();
                types.Add(effect.ApplyTarget);
            }

            return types;
        }

        private void ApplyEffects()
        {
            List<IDamageable> targetsInRange = new();
            foreach (var targetType in _affectedTargets)
            {
                targetsInRange.AddRange(GetTargetsInRangeByDamageType(targetType));
            }

            Execute(targetsInRange);
        }

        private void Execute(List<IDamageable> targets)
        {
            if (targets == null || targets.Count == 0) return;
            foreach (var effectFactory in effects)
            {
                var effect = effectFactory.Create();
                foreach (var target in targets)
                {
                    if (effect.IsTargetValid(target))
                        target.TakeEffect(effect);
                }
            }
        }

        private void OnStop()
        {
            if (onComplete != null)
            {
                onComplete();
                onComplete = null;
            }

            //NetworkServer.
            Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            var lineCollection = rangeFilter.GetRangeSector(transform);
            GizmosExtension.DrawPolygons(lineCollection);
        }

        private IEnumerable<IDamageable> GetTargetsInRangeByDamageType(DamageTarget damageTarget)
        {
            return DictRegistry<DamageTarget, Transform>
                .Filter(damageTarget, ((ITargetFilter<Transform>)rangeFilter).GetStrategy(transform))
                .Select(x => x.GetComponent<IDamageable>());

        }

        private void OnDestroy()
        {
            _timer?.Stop();
        }
    }
}