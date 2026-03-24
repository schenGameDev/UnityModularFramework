using KBCore.Refs;
using UnityEngine;
using UnityTimer;

namespace ModularFramework.Modules.Ability
{
    public class ProjectileWorldUI : MonoBehaviour
    {
        private ImpactZoneIndicator _worldIndicator;
        private Vector3 _landingPosition;
        private float _landingRadius;
        private float _runtimeRadius;
        private CountdownTimer _timer;
        
        [SerializeField, Self] private Projectile projectile;
        [SerializeField, Self] private ProjectileEffect projectileEffect;
#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif
        
        private void Start()
        {
            if (projectileEffect.isBeam)
            {
                return;
               
            }
            if  (projectileEffect.impactEffectPrefab)
            {
                ShowImpactZone(projectile.targetPosition, projectileEffect.impactEffectPrefab);
            }
            else
            {
                ShowImpactZone(true, projectile.targetPosition, 
                    projectile.collisionDetection switch
                    {
                        CastType.RAYCAST => 0.5f,
                        CastType.BOXCAST =>  Mathf.Max(projectile.halfExtents.x, projectile.halfExtents.y, projectile.halfExtents.z),
                        CastType.CAPSULECAST => projectile.radius + Vector3.Distance(projectile.pointA,  projectile.pointB) / 2,
                        CastType.SPHERECAST => projectile.radius,
                        _ => 0.5f
                    });
            }
            
        }

        private void ShowImpactZone(Vector3 targetPosition, ImpactEffect impactEffect)
        {
            ShowImpactZone(true, targetPosition, impactEffect.rangeFilter.minMaxRange.y);
            _timer = new CountdownTimer(2);
            _timer.OnTick = () =>
            {
                _worldIndicator?.ShowInWorldCoordinate(_landingPosition,_timer.Progress * _landingRadius, 
                    Color.orange);
            };
            _timer.Start();
        }
        

        private void ShowImpactZone(bool show, Vector3 landingPosition = default, float radius =0)
        {
            if (show)
            {
                _worldIndicator = PrefabPool<ImpactZoneIndicator>.Get();
                _landingPosition = landingPosition;
                _landingRadius = radius;
            }
            else if (_worldIndicator != null)
            {
                PrefabPool<ImpactZoneIndicator>.Release(_worldIndicator);
                _worldIndicator = null;
            }  
        }
        

        private void OnDisable()
        {
            if (_worldIndicator != null)
            {
                PrefabPool<ImpactZoneIndicator>.Release(_worldIndicator);  
                _worldIndicator = null;
            }
            _timer?.Dispose();
            _timer = null;
        }

        private void OnDestroy()
        {
            if (_worldIndicator != null)
            {
                PrefabPool<ImpactZoneIndicator>.Release(_worldIndicator);  
                _worldIndicator = null;
            }
            _timer?.Dispose();
            _timer = null;
        }
    }
}