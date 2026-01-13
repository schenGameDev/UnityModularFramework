using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

public interface ITargetSelector<TTarget> where TTarget : class
{
    public float GetScore(TTarget target, Transform me);

    SelectionStrategy<TTarget> GetStrategy(Transform me, int number = 1)
    {
        var strategy = this;
        return targets =>
            targets == null? new List<TTarget>() : 
                targets.OrderBy(t => strategy.GetScore(t, me))
                    .Take(number)
                    .ToList();
    }
}

public interface ITransformTargetSelector : ITargetSelector<Transform>
{
    public SelectionStrategy<T> GetStrategy<T>(Transform me, int number = 1) where T : Component
    {
        var strategy = this;
        return targets =>
            targets==null? new List<T>()
                : targets.OrderBy(t => strategy.GetScore(t.transform, me))
                    .Take(number)
                    .ToList();
    }
}


public enum SortOrder
{
    ASCENDING,
    DESCENDING
}