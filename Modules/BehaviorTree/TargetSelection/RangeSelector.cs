using System;
using UnityEngine;

[Serializable]
public struct RangeSelector : ITransformTargetSelector
{
    public SortOrder sortOrder;
    public bool SkipNegativeScore => false;

    public float GetScore(Transform target, Transform me)
    {
        return sortOrder == SortOrder.DESCENDING
            ? -Vector3.SqrMagnitude(me.position - target.position)
            : Vector3.SqrMagnitude(me.position - target.position);
    }
}