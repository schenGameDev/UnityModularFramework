using System;
using EditorAttributes;
using ModularFramework;
using UnityEngine;

public class Showable : Playable,ILive
{
    [SerializeField] private Animator animator;
    [SerializeField,ShowField(nameof(animatorExist)),AnimatorParamDropdown(nameof(animator))] private string enterTrigger;
    [SerializeField,ShowField(nameof(animatorExist)),AnimatorParamDropdown(nameof(animator))] private string exitTrigger;
    private bool animatorExist => animator != null;
    
    [field:SerializeField] public bool Live { get; set; }
    private void Awake()
    {
        Live = !disableOnAwake;
    }
    public override void Play(Action<string> callback = null,string parameter = null) {
        if(Live) return;
        Live = true;
        gameObject.SetActive(true);
        
        if(animator && !string.IsNullOrEmpty(enterTrigger)) animator.SetTrigger(enterTrigger);
        callback?.Invoke(InkConstants.TASK_PLAY_CG);
    }

    public override void End() {
        if(!Live) return;
        Live = false;
        if(animator && !string.IsNullOrEmpty(exitTrigger)) animator.SetTrigger(exitTrigger);
        else if (TryGetComponent<SpriteFadeOut>(out var fadeOut))
        {
            fadeOut.FadeOut();
        }
        else gameObject.SetActive(false);
    }
}