using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using UnityEngine;
using UnityTimer;

[DisallowMultipleComponent]
public class ImpactEffect : MonoBehaviour
{
    [SerializeField] private bool impactOverTime = false;

    [HideField(nameof(impactOverTime)), Min(0),SerializeField]
    private float waitBeforeDestroy; 
    
    [ShowField(nameof(impactOverTime)),SerializeField,Tooltip("Apply effects to target in range every n seconds"),Min(0),Suffix("s")] 
    private float tickInterval;
    [ShowField(nameof(impactOverTime)),SerializeField,Min(0)] private int ticks;
    
    [SerializeField] RangeFilter rangeFilter;
    [SerializeReference] public List<IEffectFactory<IDamageable>> effects = new();
    public Action onComplete;
    
    private Timer _timer;
    private DamageTarget _affectedTargets;
    
    //public override void OnStartServer()
    private void Start()
    {
        _affectedTargets = GetAllTargetTypes();
        if (impactOverTime)
        {
            _timer = new LimitedRepeatTimer(tickInterval, ticks);
            _timer.OnTick = ApplyEffects;
            _timer.OnTimerStop = OnStop;
            _timer.Start();
        }
        else
        {
            ApplyEffects();
            if (waitBeforeDestroy > 0)
            {
                _timer = new CountdownTimer(waitBeforeDestroy);
                _timer.OnTimerStop = OnStop;
                _timer.Start();
            }
            else
            {
                // NetworkServer.
                Destroy(gameObject);
            }
        }
    }
    
    private DamageTarget GetAllTargetTypes()
    {
        _affectedTargets = DamageTarget.None;
        foreach (var effectFactory in effects)
        {
            var effect = effectFactory.Create();
            _affectedTargets |= effect.ApplyTarget;
        }
        return _affectedTargets;
    }

    private void ApplyEffects()
    {
        List<IDamageable> targetsInRange = new();
        if(_affectedTargets.HasFlag(DamageTarget.Player))
        {
            AddTargetsInRangeByDamageType<Player>(targetsInRange);
        } 
        if(_affectedTargets.HasFlag(DamageTarget.Equipment))
        {
            AddTargetsInRangeByDamageType<Equipment>(targetsInRange);
        } 
        if(_affectedTargets.HasFlag(DamageTarget.NPC))
        {
            AddTargetsInRangeByDamageType<Npc>(targetsInRange);
        } 
        if(_affectedTargets.HasFlag(DamageTarget.Monster))
        {
            AddTargetsInRangeByDamageType<Monster>(targetsInRange);
        } 
        Execute(targetsInRange);
    }
    
    private void Execute(List<IDamageable> targets)
    {
        if(targets == null || targets.Count == 0) return;
        foreach (var effectFactory in effects)
        {
            var effect = effectFactory.Create();
            foreach (var target in targets)
            {
                if(effect.IsTargetValid(target))
                    target.TakeEffect(effect);
            }
        }
    }

    private void OnStop()
    {
        if (onComplete != null)
        {
            onComplete();
            onComplete = null;
        }

        //NetworkServer.
        Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var pointsCollection = rangeFilter.GetRangeSector(transform);
        foreach (var points in pointsCollection)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Gizmos.DrawLine(points[i], points[(i + 1) % points.Count]);
            }
        }
        
    }
    
    private void AddTargetsInRangeByDamageType<T>(List<IDamageable> targetsInRange) where T : Component
    {
        targetsInRange.AddRange(Registry<T>
            .Filter(((ITransformTargetFilter)rangeFilter).GetStrategy<T>(transform))
            .Select(x => x as IDamageable));
    }

    private void OnDestroy()
    {
        _timer?.Stop();
    }
}