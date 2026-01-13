using System;
using UnityEngine;

[Serializable]
public struct RaycastEyesightFilter : ITransformTargetFilter
{
    public LayerMask layerMask;
    public bool IsIncluded(Transform target, Transform me)
    {
        return IsRaycastHit(target, me);
    }
    

    private bool IsRaycastHit(Transform target, Transform self) {
        var dir = target.position - self.position;
        //todo: use eye position instead
        if (Physics.Raycast(self.position, dir, out RaycastHit hit, 100, layerMask))
        {
            return hit.transform == target;
        }
        return false;
    }
}

