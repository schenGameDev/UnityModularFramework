using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif

[RequireComponent(typeof(Animator))]
public class AnimationPlayer : PlayableGroup
{
    [SerializeField,Self] private Animator animator;
    [ReadOnly] public string[] animationStates;
    
#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
    
    public override void Play(Action<string> callback = null, string parameter = null) {
        base.Play(callback, parameter);
        ChangeAnimation(parameter);
    }

    private async void ChangeAnimation(string animName, float crossfade = 0.2f)
    {
        if(CurrentState == animName) return;
        if (!animationStates.Contains(animName))
        {
            DebugUtil.Warn($"Animation {animName} is not found", this.name);
            return;
        }
        CurrentState = animName;
        animator.CrossFade(animName, crossfade);
        if (OnTaskComplete != null)
        {
            await ((Func<bool>)IsAnimationFinished).WaitUntil();
            OnTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
        }
    }

    private bool IsAnimationFinished()
    {
        var currentStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return !currentStateInfo.IsName(CurrentState) || currentStateInfo.normalizedTime >= 1;
    }
    
    public override IEnumerable<string> GetStates() => animationStates;
    
#if UNITY_EDITOR
    [Button]
    private void GetAnimationStates()
    {
        var animator = GetComponent<Animator>();
        if(!animator) return;
        var animatorController = animator.runtimeAnimatorController as AnimatorController;
        if(!animatorController) return;
        List<string> states = new List<string>();
        foreach (AnimatorControllerLayer layer in animatorController.layers) 
        foreach (ChildAnimatorState childAnimatorState in layer.stateMachine.states) 
            states.Add(childAnimatorState.state.name); 
        animationStates = states.ToArray();
    }
#endif
}