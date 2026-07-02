using System;
using KBCore.Refs;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class BTAnimation : MonoBehaviour
    {
        private static readonly int STUNNED = Animator.StringToHash("Stunned");
        private static readonly int DEAD = Animator.StringToHash("Dead");
        // 0: not attack 1: windup 2: release
        private static readonly int ATTACK_STAGE = Animator.StringToHash("AttackStage");
        public static readonly int ATTACK_TYPE = Animator.StringToHash("AttackType");
        // 0: stop 1: move 2: sprint
        private static readonly int MOVE_STAGE = Animator.StringToHash("MoveStage");
        
        public static readonly string TARGET_ANGLE_NAME = "TargetAngle";
        
        [SerializeField, Self(Flag.Optional)] Animator animator;
        
#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif
        
        private Action _onAnimKeyEvent;
        
        // public void ChangeState(MonsterStatus status)
        // {
        //     bool stunned = MonsterStatusResolver.Any(status, MonsterStatus.STUNNED);
        //     bool dead = MonsterStatusResolver.Any(status, MonsterStatus.DEAD);
        //     animator.SetBool(STUNNED, stunned);
        //     animator.SetBool(DEAD, dead);
        //    
        //     if (stunned || dead)
        //     {
        //         animator.SetInteger(ATTACK_STAGE, 0);
        //         animator.SetInteger(MOVE_STAGE, 0);
        //         
        //         return;
        //     }
        //     
        //     // attack
        //     if (MonsterStatusResolver.Any(status, MonsterStatus.ATTACK_WINDUP))
        //     {
        //         animator.SetInteger(ATTACK_STAGE, 1);
        //     }
        //     else if (MonsterStatusResolver.Any(status, MonsterStatus.ATTACK_RELEASE))
        //     {
        //         animator.SetInteger(ATTACK_STAGE, 2);
        //     }
        //     else
        //     {
        //         animator.SetInteger(ATTACK_STAGE, 0);
        //     }
        //     
        //     // move
        //     bool isMoving = MonsterStatusResolver.Any(status, MonsterStatus.MOVING);
        //     bool isSprinting = MonsterStatusResolver.Any(status, MonsterStatus.SPRINTING);
        //     animator.SetInteger(MOVE_STAGE, isSprinting ? 2 : isMoving ? 1 : 0);
        //     
        // }

        private void SetFlag(AnimationConfig animationConfig)
        {
            switch (animationConfig.type)
            {
                case AnimationConfigType.NONE:
                    return;
                case AnimationConfigType.WAIT:
                {
                    float waitTime = float.Parse(animationConfig.waitTime);
                    Invoke(nameof(AnimKeyEvent), waitTime);
                    Debug.Log($"Enemy Animation wait {animationConfig.waitTime}");
                    break;
                }
                case AnimationConfigType.FLAG:
                {
                    foreach (var flagConfig in animationConfig.flags)
                    {
                        if (flagConfig.flagType == AnimationFlagType.BOOL)
                        {
                            bool value = bool.Parse(flagConfig.value);
                            animator.SetBool(flagConfig.flag, value);
                            Debug.Log($"Enemy Animation flag: {flagConfig.flag} = {flagConfig.value}");
                        } 
                        else if (flagConfig.flagType == AnimationFlagType.INT)
                        {
                            int value = int.Parse(flagConfig.value);
                            animator.SetInteger(flagConfig.flag, value);
                            Debug.Log($"Enemy Animation flag: {flagConfig.flag} = {flagConfig.value}");
                        }
                    }
                    
                    break;
                }
            }
        }
        
        public void SetFlag(AnimationConfig animationConfig, Action onKeyEvent)
        {
            _onAnimKeyEvent = onKeyEvent;
            
            SetFlag(animationConfig);
            
        }
        
        public void Wait(float seconds, Action onKeyEvent)
        {
            _onAnimKeyEvent = onKeyEvent;
            Invoke(nameof(AnimKeyEvent), seconds);
        }
        
        public void AnimKeyEvent() => _onAnimKeyEvent?.Invoke();
        
        public void ReverseFlag(AnimationConfig animationConfig, bool isInterrupt)
        {
            if (animationConfig.type == AnimationConfigType.WAIT)
            {
                Debug.Log("Enemy Animation wait stopped");
            }
            else if (animationConfig is { type: AnimationConfigType.FLAG, flags: not null })
            {
                foreach (var flagConfig in animationConfig.flags)
                {
                    if (flagConfig.flagType != AnimationFlagType.BOOL) continue;
                    if (isInterrupt && !flagConfig.reverseAtInterrupt) continue;
                    if (!isInterrupt && !flagConfig.reverseAtEnd) continue;
                    bool reverse = !bool.Parse(flagConfig.value);
                    animator.SetBool(flagConfig.flag, reverse);
                    Debug.Log($"Enemy Animation flag: {flagConfig.flag} = {reverse}");
                }
            }
            
            _onAnimKeyEvent = null;
            
        }
    }
}