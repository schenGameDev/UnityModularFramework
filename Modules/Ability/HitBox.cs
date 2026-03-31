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
    
        private void OnTriggerEnter(Collider other)
        {
            OnHitEnter?.Invoke(other.transform);
        }
    }
}