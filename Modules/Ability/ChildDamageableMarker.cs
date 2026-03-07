using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    /// <summary>
    /// Add to child collider of a damageable to mark them as damageable parts, so that it can be detected through collision
    /// </summary>
    public class ChildDamageableMarker : MonoBehaviour,IDamageable
    {
        [SerializeField,TypeFilter(typeof(IDamageable)),Required] 
        private GameObject parentDamageable;
        [SerializeField,Clamp(0,1)] private float damageMultiplier = 1; 
        // for child part that takes different damage than parent, like headshot

        private IDamageable _parent;

        private void Awake()
        {
            _parent = parentDamageable.GetComponent<IDamageable>();
        }

        public void TakeDamage(float damageAmount, DamageType damageType)
        {
            _parent.TakeDamage(damageAmount * damageMultiplier, damageType);
        }

        public void TakeEffect(IEffect<IDamageable> effect)
        {
            _parent.TakeEffect(effect); 
            // effect is not multiplied, as it can be heal or buff, and it's hard to determine how to multiply them
        }

        public void TakeSpecialCondition(SpecialCondition specialCondition)
        {
            _parent.TakeSpecialCondition(specialCondition);
        }

        public void RemoveSpecialCondition(SpecialCondition specialCondition)
        {
            _parent.RemoveSpecialCondition(specialCondition);
        }

        public void AimedAtBy(bool isAiming, Transform attacker, string details = null)
        {
            _parent.AimedAtBy(isAiming, attacker, details);
        }

        public Transform Transform => parentDamageable.transform;
        public DamageTarget TargetType => _parent.TargetType;
    }
}