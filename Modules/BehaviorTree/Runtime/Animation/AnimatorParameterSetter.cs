using System;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    /// <summary>
    /// A <see cref="StateMachineBehaviour"/> that writes a fixed value to an Animator
    /// parameter each time the owning state is entered. Supports all four Animator
    /// parameter types — Bool, Int, Float, and Trigger — selectable via
    /// <see cref="ParameterType"/>. Only the value field that corresponds to the chosen
    /// type is used at runtime; the others are ignored.
    /// </summary>
    public class AnimatorParameterSetter : StateMachineBehaviour
    {
        public enum ParameterType { BOOL, INT, FLOAT, TRIGGER }

        [SerializeField] private string parameterName;
        [SerializeField] private ParameterType parameterType;

        [SerializeField, ShowField(nameof(parameterType), ParameterType.BOOL)] 
        private bool boolValue;
        [SerializeField, ShowField(nameof(parameterType), ParameterType.INT)] 
        private int intValue;
        [SerializeField, ShowField(nameof(parameterType), ParameterType.FLOAT)] 
        private float floatValue;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            switch (parameterType)
            {
                case ParameterType.BOOL:    animator.SetBool(parameterName, boolValue);    break;
                case ParameterType.INT:     animator.SetInteger(parameterName, intValue);  break;
                case ParameterType.FLOAT:   animator.SetFloat(parameterName, floatValue);  break;
                case ParameterType.TRIGGER: animator.SetTrigger(parameterName);            break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
