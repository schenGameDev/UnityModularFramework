using System;
using System.Linq;
using EditorAttributes;
using ModularFramework;
using UnityEngine;
using UnityTimer;

public class AbilityGroundEffect : MonoBehaviour
{
    [HelpBox("Keep it empty, it will get assigned at runtime. Unless you are testing")] 
    [SerializeReference] public AbilitySO ability;

    [SerializeField] private bool impactOverTime = false;

    [HideField(nameof(impactOverTime)), Min(0),SerializeField]
    private float waitBeforeDestroy; 
    
    [ShowField(nameof(impactOverTime)),SerializeField,Tooltip("Apply effects to target in range every n seconds"),Min(0),Suffix("s")] 
    private float tickInterval;
    [ShowField(nameof(impactOverTime)),SerializeField,Min(0)] private int ticks;
    
    [SerializeField] RangeFilter rangeFilter;
    
    private LimitedRepeatTimer _timer;
    private Action _onComplete;
    [SerializeField] private bool live;
    
    private void Start()
    {
        if(!live) return;
        if (impactOverTime)
        {
            _timer = new LimitedRepeatTimer(tickInterval, ticks);
            _timer.OnTick = ApplyEffects;
            _timer.OnTimerStop = () => Destroy(gameObject);
            _timer.Start();
        }
        else
        {
            ApplyEffects();
            if (waitBeforeDestroy > 0)
            {
                Destroy(gameObject, waitBeforeDestroy);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void ApplyEffects()
    {
        var targetsInRange = Registry<Character>
            .Filter(((ITransformTargetFilter)rangeFilter).GetStrategy<Character>(transform))
            .Select(x => x as IDamageable)
            .ToList();
        ability.Execute(targetsInRange, _onComplete);
    }
    
    public void Setup(AbilitySO ability, Action onComplete)
    {
        this.ability = ability;
        _onComplete = onComplete;
        live = true;
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
}