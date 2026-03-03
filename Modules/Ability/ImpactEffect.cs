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
        [SerializeField] private bool delayBeforeStart;
        [SerializeField, ShowField(nameof(delayBeforeStart)), Min(0), Tooltip("Delay before starting impact")]
        private float delay;
        
        [SerializeField] private bool impactOverTime = false;
        [HideField(nameof(impactOverTime)), Min(0), SerializeField]
        private float waitBeforeDestroy;

        [ShowField(nameof(impactOverTime)), SerializeField, Tooltip("Apply effects to target in range every n seconds"),
         Min(0), Suffix("s")]
        private float tickInterval;

        [ShowField(nameof(impactOverTime)), SerializeField, Min(0)]
        private int ticks;

        [HelpBox("Filter ignored in beam")] 
        public RangeFilter rangeFilter;
        [SerializeField] private bool showImpactZone = true;
        [SerializeReference, SubclassSelector] public List<IEffectFactory<IDamageable>> effects = new();
        public Action onComplete;

        private Timer _timer;
        private HashSet<DamageTarget> _affectedTargets; // singular types
        
        // beam : ProjectileEffect
        private float _beamRadius;
        private uint _beamId;
        private Vector3[] _beamPoints;
        private LineRenderer _beam;
        private ImpactZoneIndicator _indicator;

        //public override void OnStartServer()
        private void Start()
        {
            _affectedTargets = GetAllTargetTypes();
            if (impactOverTime)
            {
                _timer = new LimitedRepeatTimer(tickInterval, ticks);
                _timer.OnTick = ApplyEffects;
                _timer.OnTimerStop = OnStop;
                if(delayBeforeStart) _timer.DelayStart(delay);
                else _timer.Start();
            }
            else
            {
                ApplyEffects();
                if (waitBeforeDestroy > 0)
                {
                    _timer = new CountdownTimer(waitBeforeDestroy);
                    _timer.OnTimerStop = OnStop;
                    if(delayBeforeStart) _timer.DelayStart(delay);
                    else _timer.Start();
                }
                else
                {
                    // NetworkServer.
                    Destroy(gameObject);
                }
            }
            if(showImpactZone) ShowImpactZone();
        }

        public void SetBeam(LineRenderer beam, uint beamId)
        {
            _beam = beam;
            _beamId = beamId;
            _beamRadius = _beam.startWidth / 2;
            _beamPoints = new Vector3[_beam.positionCount];
            _beam.GetPositions(_beamPoints);
        }

        private bool IsBeam => _beamPoints is { Length: > 1 };
        
        private HashSet<DamageTarget> GetAllTargetTypes()
        {
            var types = new HashSet<DamageTarget>();
            foreach (var effectFactory in effects)
            {
                if (effectFactory is HitTimeDpdtDmgEffectFactory hitTimeFactory)
                {
                    hitTimeFactory.Reset();
                }
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
                if(effectFactory is HitTimeDpdtDmgEffectFactory hitTimeFactory)
                {
                    hitTimeFactory.CreateAndApply(targets, tickInterval);
                    continue;
                }
                
                var effect = effectFactory.Create();
                foreach (var target in targets)
                {
                    if (effectFactory.IsTargetValid(target))
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
        
        private void ShowImpactZone()
        {
            if(IsBeam) return;
            if (_indicator == null)
            {
                _indicator = PrefabPool<ImpactZoneIndicator>.Get();
            }
            _indicator.ShowInLocalCoordinate(transform,Vector3.zero, Vector3.forward, rangeFilter, Color.orange);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) return;
            if(rangeFilter.minMaxRange == Vector2.zero) return;
            Gizmos.color = Color.red;
            var lineCollection = rangeFilter.GetRangeSector(transform);
            GizmosExtension.DrawPolygons(lineCollection);
        }

        private readonly RaycastHit[] _hits = new RaycastHit[10];
        private readonly Collider[] _hitColliders = new Collider[10];
        private IEnumerable<IDamageable> GetTargetsInRangeByDamageType(DamageTarget damageTarget)
        {
            List<IDamageable> targetsInRange = new ();
            if (IsBeam)
            {
                for (int i = 0; i < _beamPoints.Length - 1; i++)
                {
                    Vector3 start = _beamPoints[i];
                    Vector3 end = _beamPoints[i + 1];
                    Vector3 direction = (end - start).normalized;
                    float distance = Vector3.Distance(start, end);
                    int hitCount = Physics.SphereCastNonAlloc(start, _beamRadius, direction, _hits, distance);
                    for (int j = 0; j < hitCount; j++)
                    {
                        var hitCollider = _hits[j].collider;
                        var damageable = hitCollider.GetComponent<IDamageable>();
                        if (damageable.TargetType == damageTarget)
                        {
                            targetsInRange.Add(damageable);
                        }
                    }
                }
                return targetsInRange;
            }
            
            if (rangeFilter.rangeType is RangeFilter.RangeType.CIRCLE or RangeFilter.RangeType.CYLINDER && rangeFilter.minMaxRange.x==0)
            {
                var minHeight = rangeFilter.rangeType == RangeFilter.RangeType.CYLINDER ? rangeFilter.minMaxHeight.x : -10;
                var maxHeight = rangeFilter.rangeType == RangeFilter.RangeType.CYLINDER ? rangeFilter.minMaxHeight.y : 10;
                
                int count = Physics.OverlapCapsuleNonAlloc(transform.position + Vector3.up * minHeight, 
                        transform.position + Vector3.up * maxHeight, rangeFilter.minMaxRange.y, _hitColliders);
                for (var j = 0; j < count; j++)
                {
                    var hitCollider = _hitColliders[j];
                    var damageable = hitCollider.GetComponent<IDamageable>();
                    if (damageable.TargetType == damageTarget)
                    {
                        targetsInRange.Add(damageable);
                    }
                }
                return targetsInRange;
            }

            if (rangeFilter.rangeType is RangeFilter.RangeType.SQUARE or RangeFilter.RangeType.BOX)
            {
                var minHeight = rangeFilter.rangeType == RangeFilter.RangeType.BOX ? rangeFilter.minMaxHeight.x : -10;
                var maxHeight = rangeFilter.rangeType == RangeFilter.RangeType.BOX ? rangeFilter.minMaxHeight.y : 10;
                var boxCenter =  transform.position + (rangeFilter.minMaxRange.x + rangeFilter.minMaxRange.y) /2 * transform.forward 
                                                    + Vector3.up * (minHeight + maxHeight) / 2;
                int count = Physics.OverlapBoxNonAlloc(boxCenter, new Vector3(), _hitColliders, transform.rotation);
                for (var j = 0; j < count; j++)
                {
                    var hitCollider = _hitColliders[j];
                    var damageable = hitCollider.GetComponent<IDamageable>();
                    if (damageable.TargetType == damageTarget)
                    {
                        targetsInRange.Add(damageable);
                    }
                }
                return targetsInRange;
            }
        
            return DictRegistry<DamageTarget, Transform>
                .Filter(damageTarget, ((ITargetFilter<Transform>)rangeFilter).GetStrategy(transform))
                .Select(x => x.GetComponent<IDamageable>());

        }

        private void CleanUp()
        {
            _timer?.Stop();
            if (_beam != null)
            {
                PrefabPool<LineRenderer>.Release(_beam, _beamId);
                _beam = null;
            }

            if (_indicator != null)
            {
                PrefabPool<ImpactZoneIndicator>.Release(_indicator);
                _indicator = null;
            }
        }

        private void OnDisable()
        {
            CleanUp();
        }
        
        private void OnDestroy()
        {
            CleanUp();
        }
    }
}