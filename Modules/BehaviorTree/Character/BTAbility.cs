using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using Sisus.ComponentNames;
using UnityEngine;
using UnityTimer;

[AddComponentMenu("Behavior Tree/Ability"), RequireComponent(typeof(BTRunner))]
public class BTAbility : MonoBehaviour, IUniqueIdentifiable, IReady
{
    [Header("Targeting")]
    [SerializeReference,SubclassSelector] public ITransformTargetSelector targetSelector;
    public RangeFilter rangeFilter;
    
    [Tooltip("Number of targets"),Min(0)] 
    public int targetNumber = 1;
    [Tooltip("if targets move out of range at time of release, they will not be targeted")]
    public bool verifyRangeAtDamageTime = true;
    
    [Header("Casting")]
    [Required,SerializeField,PropertyDropdown,OnValueChanged(nameof(RenameComponent))] 
    private AbilitySO ability;
    [SerializeField, ShowField(nameof(HasOffset))] private Vector3 spawnEffectOffset;
    [SerializeField,Tooltip("Casting animation")] 
    private string animFlag;
    
    [Header("Cooldown")]
    [SerializeField,Min(0)] private float cooldown;
    
    [SerializeField,ToggleGroup("Gizmos", nameof(gizmosColor))] 
    private bool showGizmos = true;
    [SerializeField,HideProperty, HideLabel] 
    private Color gizmosColor = Color.red;
    
    [Header("Runtime")]
    [ShowInInspector, ReadOnly] private bool _isCastingAbility;
    [ReadOnly,ShowInInspector] protected bool isReady = true;
    
    public bool Ready => isReady;
    private bool HasOffset => ability is ProjectileAbilitySO or GroundEffectAbilitySO;
    
    private List<IDamageable> _targets;
    private Action<bool> _abilityReleaseCallback;
    [SerializeField,Self] private BTRunner runner;
    private CountdownTimer _cooldownTimer;
    private Vector3 _initialAimPosition;

#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
    
    private void Awake()
    {
        if (targetSelector == null)
        {
            Debug.LogWarning($"No Target Selector assigned to Ability {AbilityName}!");
        }
        
        if (ability != null)
        {
            if (ability is ProjectileAbilitySO p)
            {
                p.projectileSpawnOffset = spawnEffectOffset;
                p.RegisterProjectile();
            }
            else if(ability is GroundEffectAbilitySO g) g.groundEffectSpawnOffset = spawnEffectOffset;
        }
    }

    private void Start()
    {
        if (cooldown > 0)
        {
            _cooldownTimer = new CountdownTimer(cooldown);
            _cooldownTimer.OnTimerStart += () => isReady = false;
            _cooldownTimer.OnTimerStop += () => isReady = true;
        }
    }

    public void Cast(List<IDamageable> targets, Action<bool> callback)
    {
        if (_isCastingAbility)
        {
            Debug.LogWarning($"Casting {AbilityName} is already active, cannot cast again");
            callback(false);
            return;
        }

        if (!isReady)
        {
            Debug.LogWarning($"Ability {AbilityName} is not ready yet (on cooldown)");
            callback(false);
            return;
        }
        
        _targets = targets;
        if (_targets != null && _targets.Count > 0)
        {
            _initialAimPosition = _targets[0].Transform.position;
            runner.FaceTarget(_initialAimPosition);
        }
        _isCastingAbility = true;
        _abilityReleaseCallback = callback;
        runner.PlayAnim(animFlag, Release);
        AimAtTargets();
        ShowAbilityImpactArea();
    }
    
    private void Release()
    {
        ShowAbilityImpactArea(false);
        _targets = verifyRangeAtDamageTime
            ? _targets.Where(t =>
            {
                if (!RangeFilter.WithinGroundRange(transform, t.Transform, rangeFilter.minMaxRange))
                {
                    t.AimedAtBy(false, transform);
                    return false;
                }
                
                return true;
            }).ToList() 
            : _targets;
        if (_targets.Count == 0)
        {
            Debug.LogWarning($"{AbilityName} Missed, targets not within range");
            if (ability is ProjectileAbilitySO p) p.DryFire(transform, _initialAimPosition, null, CastComplete);
            else if (ability is GroundEffectAbilitySO g) g.DryFire(transform, _initialAimPosition, CastComplete);
            else CastComplete();
            return;
        }
        ability.Release(transform, _targets, CastComplete);
    }
    /// <summary>
    /// for continuous casting end: one effect ends, complete the cast
    /// </summary>
    private void CastComplete() 
    {
        runner.StopAnim(animFlag);
        _abilityReleaseCallback?.Invoke(true);
        AimAtTargets(false);
        _isCastingAbility = false;
        _abilityReleaseCallback = null;
        _cooldownTimer?.Start();
    }



    public void Interrupt()
    {
        runner.StopAnim(animFlag);
        _abilityReleaseCallback?.Invoke(false);
        AimAtTargets(false);
        _isCastingAbility = false;
        _abilityReleaseCallback = null;
        _cooldownTimer?.Start();
    }
    
    private void AimAtTargets(bool on = true)
    {
        foreach (var target in _targets)
        {
           target.AimedAtBy(on, transform);
        }
    }

    private void ShowAbilityImpactArea(bool on = true)
    {
        
    }

    private void OnDrawGizmos()
    {
        if (showGizmos && (_isCastingAbility || !Application.isPlaying))
        {
            Gizmos.color = gizmosColor;
            var pointsCollection = rangeFilter.GetRangeSector(transform);
            foreach (var points in pointsCollection)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    Gizmos.DrawLine(points[i], points[(i + 1) % points.Count]);
                }
            }
            
        }
    }

    public bool TargetAtSelf => ability != null && ability.targetSelf;
    
    private string AbilityName => ability == null 
        ? "Unassigned" 
        : ability.name.StartsWith("Ability_") ? ability.name[8..] : ability.name;
    
    public string UniqueId => AbilityName;
    private void RenameComponent() => this.SetName( $"Ability: {AbilityName}" + (ability && ability.continuousCasting? " (Continuous)" : "") );
}