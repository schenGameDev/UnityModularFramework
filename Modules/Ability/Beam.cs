using EditorAttributes;
using KBCore.Refs;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

namespace ModularFramework.Modules.Ability
{
    [DisallowMultipleComponent, RequireComponent(typeof(AssetIdentity))]
    public sealed class Beam : MonoBehaviour,IPrefabPoolEntry
    {
        private const string BeamEndPointPositionProperty = "BeamEndPoint_position";
        private const string DurationProperty = "Duration";

        [Header("Do not change the Scale of this object")]
        [SerializeField,Self(Flag.Optional),ReadOnly] private LineRenderer lineRenderer;
        [SerializeField,Child(Flag.Optional),ReadOnly] private TrailRenderer[] trailRenderers;
        
        [SerializeField] private VisualEffect beamVfx;
        [SerializeField, ShowField(nameof(beamVfx))] private bool disableSpawnedParticleSystems = true;
        [SerializeField, Min(0.01f), ShowField(nameof(beamVfx))] private float minimumBeamLength = 0.05f;
        [SerializeField, Min(0.01f), ShowField(nameof(beamVfx))] private float vfxDuration = 999f;
        [SerializeField, ShowField(nameof(beamVfx))] private bool restartVfxOnEnable = true;
        [SerializeField, ShowField(nameof(beamVfx))] private Vector3 localEndPointOffset;
        
        private Vector3[] _positions;
        
        public Vector3[] Positions => _positions;
        
    #if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
    #endif
        
        private void Awake()
        {
            SetUpVfx();
        }

        private void OnEnable()
        {
            PlayVfx();
        }

        private void OnDisable()
        {
            if (trailRenderers != null)
            {
                foreach (var trailRenderer in trailRenderers)
                {
                    trailRenderer.Clear();
                }
            }
        }

        public void SetPositions(Vector3[] points)
        {
            if (points.Length < 2) return;
            
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions(points);
                Debug.Log($"Beam lengths: {name} {points.Length}");
            }

            if (beamVfx != null)
            {
                
                Vector3 start = points[0];
                Vector3 end = points[^1];
                Vector3 delta = end - start;
                Vector3 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : transform.forward;
                float length = Mathf.Max(delta.magnitude, minimumBeamLength);
                // Debug.Log($"Beam length: {length}");
                beamVfx.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction, Vector3.up));
                
                Vector3 localEndPoint = new Vector3(0f, 0f, length) + localEndPointOffset;
                if (beamVfx.HasVector3(BeamEndPointPositionProperty))
                {
                    beamVfx.SetVector3(BeamEndPointPositionProperty, localEndPoint);
                }

                if (beamVfx.HasFloat(DurationProperty))
                {
                    beamVfx.SetFloat(DurationProperty, vfxDuration);
                }
            }
        }
        
        public void SetPositionsAndColor(Vector3[] points, Color color)
        {
            if (points.Length < 2) return;
            
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = points.Length;
                lineRenderer.SetPositions(points);
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;

            }

            if (beamVfx != null)
            {
                Vector3 start = points[0];
                Vector3 end = points[^1];
                Vector3 delta = end - start;
                Vector3 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : transform.forward;
                float length = Mathf.Max(delta.magnitude, minimumBeamLength);

                beamVfx.transform.SetPositionAndRotation(start, Quaternion.LookRotation(direction, Vector3.up));

                Vector3 localEndPoint = new Vector3(0f, 0f, length) + localEndPointOffset;
                if (beamVfx.HasVector3(BeamEndPointPositionProperty))
                {
                    beamVfx.SetVector3(BeamEndPointPositionProperty, localEndPoint);
                }

                if (beamVfx.HasFloat(DurationProperty))
                {
                    beamVfx.SetFloat(DurationProperty, vfxDuration);
                }
            }
        }

        private void SetUpVfx()
        {
            if (disableSpawnedParticleSystems)
            {
                DisableParticleSystems();
            }
            
            if (beamVfx == null)
            {
                return;
            }
            
            if (beamVfx.HasFloat(DurationProperty))
            {
                beamVfx.SetFloat(DurationProperty, vfxDuration);
            }
        }
        
        private void DisableParticleSystems()
        {
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }

        private void PlayVfx()
        {
            if (beamVfx == null)
            {
                return;
            }

            if (restartVfxOnEnable)
            {
                beamVfx.Reinit();
            }

            beamVfx.Play();
        }
        
        #region Beam Pool Entry
        public void RegisterPrefabToPool()
        {
            PrefabPool<Beam>.Register(this, CreateBeamPool);
        }

        public void ClearPool()
        {
            PrefabPool<Beam>.Clear();
        }
        
        private ObjectPool<Beam> CreateBeamPool(uint assetId, Beam prefab)
        {
            return new ObjectPool<Beam>(
                createFunc: () =>
                {
                    var beam = Instantiate(prefab,Vector3.zero, Quaternion.identity, SingletonRegistry<ProjectileManagerSO>.Instance.effectParent);
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
        #endregion
    }
}
