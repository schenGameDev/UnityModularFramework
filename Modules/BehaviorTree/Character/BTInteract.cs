using System;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using Sisus.ComponentNames;
using UnityEngine;

[AddComponentMenu("Behavior Tree/Interact"), RequireComponent(typeof(BTRunner))]
public abstract class BTInteract: MonoBehaviour, IUniqueIdentifiable
{
    [SerializeField,OnValueChanged(nameof(RenameComponent))] protected string interactName;
    [SerializeField] private string animFlag;
    [Header("Runtime")]
    [ShowInInspector, ReadOnly] private bool _isInteracting;

    protected Transform target;
    private Action<bool> _interactCallback;
    [SerializeField,Self]private BTRunner runner;
    
#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
    
    private void RenameComponent() => this.SetName($"Interact: {interactName}");
    
    public virtual void Interact(Transform target, Action<bool> callback)
    {
        this.target = target;
        if(_isInteracting) return;
        _isInteracting = true;
        _interactCallback = callback;
        runner.PlayAnim(animFlag, Stop);
    }

    protected abstract void InteractResult();
    
    public virtual void Stop()
    {
        if(!_isInteracting) return;
        _isInteracting = false;
        InteractResult();
        _interactCallback?.Invoke(true);
    }

    public string UniqueId => interactName;
}