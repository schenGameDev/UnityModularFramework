using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

public interface ITargetFilter<TTarget> where TTarget : class
{
    bool IsIncluded(TTarget target, Transform me);

    FilterStrategy<TTarget> GetStrategy(Transform me)
    {
        var filter = this;
        return target => filter.IsIncluded(target, me);
    }
}

public interface ITransformTargetFilter : ITargetFilter<Transform>
{
    FilterStrategy<T> GetStrategy<T>(Transform me) where T : Component
    {
        var filter = this;
        return target =>  target!=null && filter.IsIncluded(target.transform, me);
    }
    
    public static IEnumerable<Transform> Filter(IEnumerable<Transform> source, Transform me, 
        params ITransformTargetFilter[] targetFilters)
    {
        var filters = targetFilters == null || targetFilters.Length == 0 
            ? null :  targetFilters.Select(f => f.GetStrategy(me)).ToArray();
        return filters==null ? source : 
            source.Where(target => filters.All(filter => filter(target)));
    }
}