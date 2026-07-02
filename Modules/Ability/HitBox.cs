using System;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    /// <summary>
    /// Attach onto attacker, gameObject must have a trigger collider. Be aware of same attack hit more than once
    /// </summary>
    public class HitBox : MonoBehaviour
    {
        public Action<Transform> OnHitEnter;
        
        private float _lastHitTime;
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }
    
        private void OnTriggerEnter(Collider other)
        {
            OnHitEnter?.Invoke(other.transform);
            _lastHitTime = Time.time;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_collider == null)
                return;

            bool recentlyHit = Application.isPlaying && (Time.time - _lastHitTime) < 2f;
        
            if (!recentlyHit) return;

            Gizmos.color = Color.red;

            Gizmos.matrix = transform.localToWorldMatrix;

            if (_collider is BoxCollider box)
            {
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (_collider is SphereCollider sphere)
            {
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }
            else if (_collider is CapsuleCollider capsule)
            {
                // Approximate capsule with a wire sphere at its center
                Gizmos.DrawWireSphere(capsule.center, capsule.radius);
            }
            else if (_collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
            {
                Gizmos.DrawWireMesh(meshCollider.sharedMesh);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}