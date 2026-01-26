using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

public interface ITargetSelector<TTarget> where TTarget : class
{
    float GetScore(TTarget target, Transform me);
    bool SkipNegativeScore { get; }

    SelectionStrategy<TTarget> GetStrategy(Transform me, int number = 1)
    {
        var strategy = this;
        if (SkipNegativeScore)
        {
            return targets =>
                targets == null? new List<TTarget>() : 
                    targets.Select(t => (t,strategy.GetScore(t, me)))
                        .Where(t => t.Item2 >= 0)
                        .OrderBy(t => t.Item2)
                        .Take(number)
                        .Select(t => t.Item1)
                        .ToList();
        }
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
        if (SkipNegativeScore)
        {
            return targets =>
                targets==null? new List<T>()
                    : targets.Select(t => (t,strategy.GetScore(t.transform, me)))
                        .Where(t => t.Item2 >= 0)
                        .OrderBy(t => t.Item2)
                        .Take(number)
                        .Select(t => t.Item1)
                        .ToList();
        }
        
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