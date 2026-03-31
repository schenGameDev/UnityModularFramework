using EditorAttributes;
using ModularFramework.Modules.Ability;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("Behavior Tree/Ability/Instant"), RequireComponent(typeof(BTRunner))]
    public class BTAbilityInstant : BTAbility
    {
        protected override bool VerifyRangeAtDamageTime => false;
        
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