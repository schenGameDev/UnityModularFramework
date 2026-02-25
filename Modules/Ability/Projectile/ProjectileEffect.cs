using System;
using System.Collections.Generic;
using EditorAttributes;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Pool;

namespace ModularFramework.Modules.Ability
{
    [DisallowMultipleComponent, RequireComponent(typeof(Projectile))]
    public class ProjectileEffect : MonoBehaviour
    {
        [SerializeField, PropertyDropdown] public ImpactEffect impactEffectPrefab;

        [SerializeReference, SubclassSelector, HideField(nameof(impactEffectPrefab))]
        public List<IEffectFactory<IDamageable>> effects = new();
        [SerializeField] private DamageTarget penetrate;
        [SerializeField] private bool isBeam;
        [ShowField(nameof(isBeam)),HelpBox("must have assetIdentity")] public LineRenderer beamPrefab;
        [SerializeField, Self] private Projectile projectile;
        
        public Action onComplete;

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif

        private void Awake()
        {
            CreateBeam();
        }

        #region Projectile
        public bool Arrive(Transform target, Vector3 hitPoint)
        {
            // target like ground can be too big,
            // so we use hitPoint to spawn effect, and use target to get IDamageable
            bool isPenetrate = IsPenetrate(target);
            if (impactEffectPrefab != null)
            {
                var impactEffect = Instantiate(impactEffectPrefab, hitPoint, Quaternion.identity,
                    SingletonRegistry<ProjectileManagerSO>.Instance.effectParent);
                if (isBeam)
                {
                    impactEffect.SetBeam(_beam, _beamId);
                    _beam = null;
                }
            }
            else
            {
                Execute(target == null ? null : target.GetComponent<IDamageable>(), isPenetrate);
            }

            return !isPenetrate;
        }

        private bool IsPenetrate(Transform target)
        {
            if (!target.TryGetComponent(out IDamageable damageable)) return false;
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
                target.TakeEffect(effectFactory.Create());
            }
        }
        #endregion
        #region Beam

        private LineRenderer _beam;
        private uint _beamId;
        private void CreateBeam()
        {
            if (!isBeam || _beam != null)
            {
                return;
            }
            
            _beamId = beamPrefab.GetComponent<AssetIdentity>().assetId;
            if (!PrefabPool<LineRenderer>.TryGet(_beamId, out var beam))
            {
                PrefabPool<LineRenderer>.Register(beamPrefab, CreateBeamPool);
            }
    
            if(beam == null) return;
            beam.SetPositions(projectile.GetTrajectory());
            _beam = beam;
        }
        
        private ObjectPool<LineRenderer> CreateBeamPool(uint assetId, LineRenderer prefab)
        {
            return new ObjectPool<LineRenderer>(
                createFunc: () =>
                {
                    var beam = Instantiate(prefab);
                    beam.gameObject.SetActive(false);
                    return beam;
                },
                actionOnGet: beam => beam.gameObject.SetActive(true),
                actionOnRelease: beam => beam.gameObject.SetActive(false),
                actionOnDestroy: beam => Destroy(beam.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }
        
        private void RemoveBeam()
        {
            if (_beam != null)
            {
                PrefabPool<LineRenderer>.Release(_beam, _beamId);
                _beam = null;
            }
        }
    
        private void Update()
        {
            if (!isBeam || _beam == null) return;
            var points = projectile.GetTrajectory();
            if (points.Length < 2) return;
            _beam.SetPositions(points);
        }

        private void OnDisable()
        {
            RemoveBeam();
        }

        private void OnDestroy()
        {
            RemoveBeam();
        }
        #endregion
    }
}