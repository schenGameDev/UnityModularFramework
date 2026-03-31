using System.Collections.Generic;
using EditorAttributes;
using ModularFramework.Modules.Ability;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("Behavior Tree/Ability/Melee"), RequireComponent(typeof(BTRunner))]
    public class BTAbilityMelee : BTAbility
    {
        
        [SerializeField] private HitBox[] hitBoxes;
    
        private List<IDamageable> _damagedTargets = new List<IDamageable>(); // prevent hit one object twice
        
        protected override bool VerifyRangeAtDamageTime => false;

        protected override void Release()
        {
            var useHitBox = TurnOnHitBox(true);
            if (!useHitBox)
            {
                base.Release();
                return;
            }
            runner.PlayAnim(releaseAnimation, () => CastComplete(ability));
        
        }
        
        protected override void CastComplete(AbilitySO abilitySo)
        {
            TurnOnHitBox(false);
            base.CastComplete(abilitySo);
        }

        private bool TurnOnHitBox(bool turnOn)
        {
            _damagedTargets.Clear();
            if (hitBoxes != null && hitBoxes.Length > 0)
            {
                foreach (var hb in hitBoxes)
                {

                    hb.OnHitEnter = turnOn ? OnHitBoxEnter : null;
                }
                return true;
            }
            return false;
        }
    
        private void OnHitBoxEnter(Transform hit)
        {
            var damageable = hit.GetComponent<IDamageable>();
            if (damageable == null || _damagedTargets.Contains(damageable)) return;
            _damagedTargets.Add(damageable);
            ability.Release(transform, new () {damageable}, null);
        }

        #region Editor
        protected override ValidationCheck ValidateAbility()
        {
            var baseResult = base.ValidateAbility();
            if (!baseResult.PassedCheck) return baseResult;
            if (ability is InstantAbilitySO)
            {
                return ValidationCheck.Pass();
            }
            return ValidationCheck.Fail("Ability needs to be of type InstantAbilitySO");
            
        }
        #endregion
    }
}