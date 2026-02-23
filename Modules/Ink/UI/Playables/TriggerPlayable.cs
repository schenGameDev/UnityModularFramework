using System;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.Ink
{
    public class TriggerPlayable : TimedPlayable
    {
        [SerializeField] private Animator animator;

        [SerializeField, AnimatorParamDropdown(nameof(animator))]
        private string enterTrigger;

        public override void Play(Action<string> callback = null, string parameter = null)
        {
            base.Play(callback, parameter);
            animator.SetTrigger(enterTrigger);
        }
    }
}