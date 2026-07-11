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
        
        [SerializeField] private MoveWith moveWith = MoveWith.Static;
        [Tooltip("the caster will be spared when applying effects")]
        public bool ignoreCaster;
        
        enum MoveWith {Static, Caster, Target}
        
        // Beam
        [ToggleGroup("Beam", nameof(beamMaxRange), nameof(extendable), nameof(beamRadius))]
        [SerializeField] private bool isBeam;
        
        [ReadOnly,HideProperty] public float beamMaxRange = 0;
        [SerializeField, Tooltip("Only applicable to straight beam"), HideProperty]
        private bool extendable;
        [SerializeField, HideProperty] private float beamRadius;

        [HideField(nameof(isBeam)),HelpBox("Filter ignored in beam")] 
        public RangeFilter rangeFilter;
        
        [SerializeField] private bool showImpactZone = true;
        [SerializeReference, SubclassSelector] public List<IEffectFactory<IDamageable>> effects = new();
        public Action onComplete;
        
        private Timer _effectTimer;
        private HashSet<DamageTarget> _affectedTargets; // singular types
        
        // beam : ProjectileEffect
        private uint _beamId;
        private Vector3[] _beamPoints;
        private Beam _beam;
        
        
        private ImpactZoneIndicator _indicator;
        private LayerMask _layerMask;
        
        private IDamageable _caster; 
        private Transform _target;
        private Quaternion _relativeRotation;
        private Vector3 _relativePosition;

        private void Awake()
        {
            _layerMask = Physics.AllLayers;
        }
        
        //public override void OnStartServer()
        private void Start()
        {
            _affectedTargets = GetAllTargetTypes();
            
            if (moveWith != MoveWith.Static || isBeam)
            {
                TimerManager.Tick += UpdatePositionAndRotation;
            }
            
            if (impactOverTime)
            {
                _effectTimer = new LimitedRepeatTimer(tickInterval, ticks);
                
                _effectTimer.OnTick += ApplyEffects;
                _effectTimer.OnTimerStop = OnStop;
                if(delayBeforeStart) _effectTimer.DelayStart(delay);
                else _effectTimer.Start();
            }
            else
            {
                if (waitBeforeDestroy > 0)
                {
                    _effectTimer = new CountdownTimer(waitBeforeDestroy);
                    _effectTimer.OnTimerStart = ApplyEffects;
                    _effectTimer.OnTimerStop = OnStop;
                    if(delayBeforeStart) _effectTimer.DelayStart(delay);
                    else _effectTimer.Start();
                }
                else
                {
                    // NetworkServer.
                    Destroy(gameObject);
                }
            }
            if(showImpactZone) ShowImpactZone();
        }

        public void SetBeam(Beam beam, uint beamId)
        {
            _beam = beam;
            _beamId = beamId;
            _beamPoints = _beam.Positions;
        }
        
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
                    hitTimeFactory.CreateAndApply(targets, tickInterval, transform);
                    continue;
                }
                
                var effect = effectFactory.Create();
                foreach (var target in targets)
                {
                    if (effectFactory.IsTargetValid(target))
                        target.EffectResolver.TakeEffect(effect, transform);
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

            _effectTimer = null;
            //NetworkServer.
            Destroy(gameObject);
        }
        
        private void ShowImpactZone()
        {
            if(isBeam) return;
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
            if (isBeam)
            {
                for (int i = 0; i < _beamPoints.Length - 1; i++)
                {
                    Vector3 start = _beamPoints[i];
                    Vector3 end = _beamPoints[i + 1];
                    Vector3 direction = (end - start).normalized;
                    float distance = Vector3.Distance(start, end);
                    int hitCount = Physics.SphereCastNonAlloc(start, beamRadius, direction, _hits, distance,_layerMask);
                    for (int j = 0; j < hitCount; j++)
                    {
                        var hitCollider = _hits[j].collider;
                        var damageable = hitCollider.GetComponent<IDamageable>();
                        if (damageable.TargetType == damageTarget
                            && (!ignoreCaster || damageable != _caster))
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
                        transform.position + Vector3.up * maxHeight, rangeFilter.minMaxRange.y, _hitColliders, _layerMask);
                for (var j = 0; j < count; j++)
                {
                    var hitCollider = _hitColliders[j];
                    var damageable = hitCollider.GetComponent<IDamageable>();
                    if (damageable.TargetType == damageTarget
                        && (!ignoreCaster || damageable != _caster))
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
                int count = Physics.OverlapBoxNonAlloc(boxCenter, new Vector3(), _hitColliders, transform.rotation, _layerMask);
                for (var j = 0; j < count; j++)
                {
                    var hitCollider = _hitColliders[j];
                    var damageable = hitCollider.GetComponent<IDamageable>();
                    if (damageable.TargetType == damageTarget
                        && (!ignoreCaster || damageable != _caster))
                    {
                        targetsInRange.Add(damageable);
                    }
                }
                return targetsInRange;
            }

            return DictSetRegistry<DamageTarget, Transform>
                .Filter(damageTarget, ((ITargetFilter<Transform>)rangeFilter).GetStrategy(transform))
                .Select(x => x.GetComponent<IDamageable>())
                .Where(damageable => !ignoreCaster || damageable != _caster);

        }
        
        public void SetCasterAndTargets(Transform caster, Transform target)
        {
            if (caster == null)
            {
                _caster = null;
            }
            else
            {
                _caster = caster.GetComponent<IDamageable>();
                if (moveWith == MoveWith.Caster && _caster != null)
                {
                    _relativeRotation = Quaternion.Inverse(caster.rotation) * transform.rotation;
                    _relativePosition = transform.position -  caster.position;
                }
            }

            if (target != null && moveWith == MoveWith.Target)
            {
                _target = target;
                _relativeRotation = Quaternion.Inverse(target.rotation) * transform.rotation;
                _relativePosition = transform.position -  target.position;
            }
            
        }

        private void UpdatePositionAndRotation(float deltaTime)
        {
            if (moveWith != MoveWith.Static)
            {
                var anchor = _caster != null && moveWith == MoveWith.Caster 
                    ? _caster.Transform 
                    : _target != null && moveWith == MoveWith.Target? _target : null;
                if (anchor != null)
                {
                    var targetRot = anchor.rotation * _relativeRotation;
                    var targetPos = anchor.position + _relativePosition;
                    // update beam points
                    if (_beamPoints != null)
                    {
                        var deltaRot = targetRot * Quaternion.Inverse(transform.rotation);
                        var deltaPos = targetPos - transform.position;
                        for (int i = 0; i < _beamPoints.Length; i++)
                        {
                            _beamPoints[i] = deltaRot * (_beamPoints[i] - transform.position) + transform.position + deltaPos;
                        }
                    }
        
                    transform.rotation = targetRot;
                    transform.position = targetPos;
                }
                
            }

            if (_beam != null)
            {
                UpdateBeam();
            }
            
        }

        #region Beam
        private void UpdateBeam()
        {
            if (isBeam && _beam != null)
            {
                if (_beamPoints.Length < 2) return;
                if (extendable)
                {
                    ExtendBeam();
                    _beam.SetPositions(_beamPoints);
                }
            }
        
        }
        
        private void ExtendBeam()
        {
            var direction = _beamPoints[^1] - _beamPoints[0];

            if (Physics.Raycast(_beamPoints[0], direction, out var hit, beamMaxRange, _layerMask))
            {

                var newEnd = hit.point + direction.normalized * 0.1f;// prevent z-fighting
                _beamPoints = new [] {_beamPoints[0], newEnd}; 
            }
            else
            {
                _beamPoints = new [] {_beamPoints[0], direction.normalized * beamMaxRange + _beamPoints[0]};
            }
        }
        

        #endregion

        private void CleanUp()
        {
            _effectTimer?.Stop();
            TimerManager.Tick -= UpdatePositionAndRotation;
            if (_beam != null)
            {
                PrefabPool<Beam>.Release(_beam, _beamId);
                _beam = null;
            }
            _beamId = 0;
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