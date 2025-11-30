using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
#if UNITY_EDITOR
using UnityEditor.Animations;
#endif
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationPlayer : PlayableGroup
{
    private Animator _animator;
    [ReadOnly] public string[] animationStates;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }
    
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
        _animator.CrossFade(animName, crossfade);
        if (OnTaskComplete != null)
        {
            await ((Func<bool>)IsAnimationFinished).WaitUntil();
            OnTaskComplete?.Invoke(InkConstants.TASK_PLAY_CG);
        }
    }

    private bool IsAnimationFinished()
    {
        var currentStateInfo = _animator.GetCurrentAnimatorStateInfo(0);
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