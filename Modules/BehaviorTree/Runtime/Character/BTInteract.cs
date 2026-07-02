using System;
using EditorAttributes;
using KBCore.Refs;
using Sisus.ComponentNames;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [AddComponentMenu("Behavior Tree/Interact"), RequireComponent(typeof(BTRunner))]
    public abstract class BTInteract : MonoBehaviour, IUniqueIdentifiable
    {
        [SerializeField, OnValueChanged(nameof(RenameComponent))]
        protected string interactName;

        [SerializeField] private AnimationConfig animConfig;

        [Header("Runtime")] [ShowInInspector, ReadOnly]
        private bool _isInteracting;

        protected Transform target;
        private Action<bool> _interactCallback;
        [SerializeField, Self] private BTAnimation animation;

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif

        private void RenameComponent() => this.SetName($"Interact: {interactName}");

        public virtual void Interact(Transform target, Action<bool> callback)
        {
            this.target = target;
            if (_isInteracting) return;
            _isInteracting = true;
            _interactCallback = callback;
            animation.SetFlag(animConfig, () => Stop(false));
        }

        protected abstract void InteractResult();

        public void Interrupt()
        {
            Stop(true);
        }

        protected virtual void Stop(bool isInterrupt)
        {
            if (!_isInteracting) return;
            _isInteracting = false;
            InteractResult();
            _interactCallback?.Invoke(true);
            animation.ReverseFlag(animConfig, isInterrupt);
        }

        public string UniqueId => interactName;
    }
}