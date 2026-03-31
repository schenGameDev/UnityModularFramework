using ModularFramework.Modules.Ability;
using ModularFramework.Modules.Targeting;
using UnityEngine;
using UnityEngine.Pool;

namespace ModularFramework.Modules.BehaviorTree
{
    public class BTWorldUI : MonoBehaviour
    {
        [SerializeField] private LineRenderer[] trajectoryLinePrefabs;
        
        private ImpactZoneIndicator _localIndicator;
        private ImpactZoneIndicator _worldIndicator;
        private LineRenderer _lineIndicator;
        private bool _isUpdateWorldIndicator;

        private uint _lineId;
        
        private void Start()
        {
            if (trajectoryLinePrefabs != null || trajectoryLinePrefabs.Length > 0)
            {
                foreach (var linePrefab in trajectoryLinePrefabs)
                {
                    PrefabPool<LineRenderer>.Register(linePrefab, CreateLinePool);
                }
            }
        }
        
        private ObjectPool<LineRenderer> CreateLinePool(uint assetId, LineRenderer prefab)
        {
            return new ObjectPool<LineRenderer>(
                createFunc: () =>
                {
                    var line = Instantiate(prefab,Vector3.zero, Quaternion.identity);
                    line.gameObject.SetActive(false);
                    return line;
                },
                actionOnGet: line => line.gameObject.SetActive(true),
                actionOnRelease: line => line.gameObject.SetActive(false),
                actionOnDestroy: line => Destroy(line.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        public void ShowImpactAreaLocal(RangeFilter rangeFilter)
        {
            if (_localIndicator == null)
            {
                _localIndicator = PrefabPool<ImpactZoneIndicator>.Get();
            }
            _localIndicator.ShowInLocalCoordinate(transform,Vector3.zero, Vector3.forward, rangeFilter, Color.orange);
        }

        public void HideImpactAreaLocal()
        {
            if (_localIndicator == null) return;
            PrefabPool<ImpactZoneIndicator>.Release(_localIndicator);
            _localIndicator = null;
        }
        
        public void ShowImpactAreaWorld(Vector3 position, RangeFilter rangeFilter, bool isUpdateEachFrame)
        {
            if (_worldIndicator == null)
            {
                _worldIndicator = PrefabPool<ImpactZoneIndicator>.Get();
            }
            _worldIndicator.ShowInWorldCoordinate(position, transform.forward, rangeFilter, Color.orange);
            _isUpdateWorldIndicator = isUpdateEachFrame;
        }

        public void HideImpactAreaWorld()
        {
            if (_worldIndicator == null) return;
            PrefabPool<ImpactZoneIndicator>.Release(_worldIndicator);
            _worldIndicator = null;
        }

        public void ShowTrajectory(uint lineAssetId, Vector3[] points)
        {
            if (lineAssetId == 0) return;
            if (_lineIndicator == null)
            {
                _lineIndicator = PrefabPool<LineRenderer>.Get(lineAssetId);
                _lineId = lineAssetId;
            }
            
            _lineIndicator.positionCount = points.Length;
            _lineIndicator.SetPositions(points);
        }
        
        public void UpdateTrajectory(Vector3[] points)
        {
            if (_lineIndicator == null) return;
            _lineIndicator.positionCount = points.Length;
            _lineIndicator.SetPositions(points);
        }
        
        public void HideTrajectory()
        {
            if (_lineIndicator == null) return;
            PrefabPool<LineRenderer>.Release(_lineIndicator, _lineId);
        }

        private void Update()
        {
            if (_localIndicator != null)
            {
                _localIndicator.UpdateFaceDirection();
            }

            if (_isUpdateWorldIndicator && _worldIndicator != null)
            {
                _worldIndicator.UpdateFaceDirection();
                _worldIndicator.UpdatePosition(transform.position);
            }
        }
        
        private void OnDisable()
        {
            HideImpactAreaLocal();
            HideImpactAreaWorld();
        }

        private void OnDestroy()
        {
            HideImpactAreaLocal();
            HideImpactAreaWorld();
        }
    }
}