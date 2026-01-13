using System;
using EditorAttributes;
using ModularFramework;
using Sisus.ComponentNames;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public abstract class EnemyInteract: MonoBehaviour, IMultiComponent<EnemyInteract>
{
    [SerializeField,OnValueChanged(nameof(RenameComponent))] protected string interactName;
    [SerializeField] private string animFlag;
    [Header("Runtime")]
    [ShowInInspector, ReadOnly] private bool _isInteracting;

    protected Transform target;
    private Action<bool> _interactCallback;
    private BTRunner _runner;
    
    private void RenameComponent() => this.SetName($"Interact: {interactName}");

    private void Awake()
    {
        _runner = GetComponent<BTRunner>();
    }

    public virtual void Interact(Transform target, Action<bool> callback)
    {
        this.target = target;
        if(_isInteracting) return;
        _isInteracting = true;
        _interactCallback = callback;
        _runner.PlayAnim(animFlag, Stop);
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