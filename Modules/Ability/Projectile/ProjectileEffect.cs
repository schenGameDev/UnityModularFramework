using System;
using System.Collections.Generic;
using EditorAttributes;
using KBCore.Refs;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    [DisallowMultipleComponent, RequireComponent(typeof(Projectile))]
    public class ProjectileEffect : MonoBehaviour
    {
        [SerializeField, PropertyDropdown] public ImpactEffect impactEffectPrefab;

        [SerializeReference, SubclassSelector, HideField(nameof(impactEffectPrefab))]
        public List<IEffectFactory<IDamageable>> effects = new();
        [SerializeField] private DamageTarget penetrate;
        
        
        public Beam beamPrefab;
        public bool IsBeam => beamPrefab != null;
        private uint _beamId;
        
        public bool ignoreCaster;
        [HideInInspector] public Transform caster; 
        
        [SerializeField, Self] private Projectile projectile;
        [SerializeField, Child(Flag.Optional)] private TrailRenderer[] trailRenderers;
        
        public Action onComplete;

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif

        private void Awake()
        {
            if (beamPrefab != null)
            {
                _beamId = beamPrefab.GetComponent<AssetIdentity>().assetId;
            }
        }
        
        private void OnEnable()
        {
            CreateBeam();
        }

        #region Projectile
        public bool Arrive(Transform target, Vector3 hitPoint)
        {
            if (!projectile.Started)
            {
                if (target == null)
                {
                    // end of life
                    onComplete?.Invoke();
                }
                return false;
            }

            bool isPenetrate = false;
            if (target != null && target.TryGetComponent(out IDamageable damageable))
            {
                if (ignoreCaster && damageable.Transform == caster)
                {
                    return false;
                }
                isPenetrate = IsPenetrate(damageable);
            }
            // target like ground can be too big,
            // so we use hitPoint to spawn effect, and use target to get IDamageable
             
            if (!isPenetrate && impactEffectPrefab != null)
            {
                var impactEffect = Instantiate(impactEffectPrefab, hitPoint, Quaternion.identity,
                    SingletonRegistry<ProjectileManagerSO>.Instance.effectParent);
                if (IsBeam)
                {
                    UpdateBeam(true);
                    impactEffect.SetBeam(_beam, _beamId);
                    _beam = null;
                }
                impactEffect.SetCasterAndTargets(caster, target);
                impactEffect.onComplete = onComplete;
            }
            else
            {
                Execute(target == null ? null : target.GetComponent<IDamageable>(), isPenetrate);
            }

            return !isPenetrate;
        }

        private bool IsPenetrate(IDamageable damageable)
        {
            return penetrate.HasFlag(damageable.TargetType);
        }

        private void Execute(IDamageable target, bool isPenetrate)
        {
            if (!isPenetrate && onComplete != null)
            {
                onComplete();
                onComplete = null;
            }

            if (target == null) return;
            foreach (var effectFactory in effects)
            {
                if (!effectFactory.IsTargetValid(target)) continue;
                target.EffectResolver.TakeEffect(effectFactory.Create(), transform);
            }
        }
        #endregion
        #region Beam

        private Beam _beam;
        private void CreateBeam()
        {
            if (!IsBeam || _beam != null)
            {
                return;
            }
            
            if (PrefabPool<Beam>.TryGet(_beamId, out var beam))
            {
                var points = projectile.GetTrajectory();
                beam.SetPositions(points);
                _beam = beam;
            }
            else
            {
                Debug.LogWarning($"Beam prefab with id {_beamId} not found in pool. Make sure to register the prefab to the pool before using it.");
            }
           
        }

        private void UpdateBeam(bool arrived)
        {
            if (!IsBeam || _beam == null) return;
            var points = projectile.GetTrajectory();
            if (points.Length < 2) return;
            if (arrived)
            {
                points[^1] += transform.forward * 0.1f; // prevent z-fighting
            }
            _beam.SetPositions(points);
        }
    
        private void Update()
        {
            UpdateBeam(false);
        }
        #endregion
        
        private void CleanUp()
        {
            if (_beam != null)
            {
                PrefabPool<Beam>.Release(_beam, _beamId);
                _beam = null;
            }

            caster = null;
            
            if (trailRenderers != null)
            {
                foreach (var trailRenderer in trailRenderers)
                {
                    trailRenderer.Clear();
                }
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