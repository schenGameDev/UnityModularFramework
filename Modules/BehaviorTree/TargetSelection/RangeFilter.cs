using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using UnityEngine;

[Serializable]
public struct RangeFilter : ITransformTargetFilter
{
    public RangeType rangeType;
    [Clamp(0,999,0,999)] public Vector2 minMaxRange;
    [Range(0,360)] public int viewAngle;

    [ShowField(nameof(rangeType), RangeType.CYLINDER)]
    public Vector2 minMaxHeight;
    
    
    /// <summary>
    /// when me is null, ignore view cone
    /// </summary>
    /// <param name="target"></param>
    /// <param name="me"></param>
    /// <returns></returns>
    public bool IsIncluded(Transform target, Transform me = null) 
    {
        return WithinGroundRange(target, me, minMaxRange) && WithinViewCone(target, me, viewAngle / 2) &&
               (rangeType == RangeType.CIRCLE || minMaxHeight.x>=minMaxHeight.y || WithinHeightRange(target, me, minMaxHeight));
    }
    
    public static bool WithinGroundRange(Transform a, Transform b, Vector2 minMaxRange)
    {
        if(!a || !b) return false;
        var diff = a.position - b.position;
        diff.y = 0;
        var sqrDist = Vector3.SqrMagnitude(diff);
        return sqrDist <= minMaxRange.y * minMaxRange.y && 
               sqrDist >= minMaxRange.x * minMaxRange.x;
    }
    
    public static bool WithinHeightRange(Transform target, Transform me, Vector2 minMaxHeight)
    {
        if(!target || !me) return false;
        var heightDiff = target.position.y - me.position.y;
        return heightDiff <= minMaxHeight.y && 
               heightDiff >= minMaxHeight.x;
    }
    
    public static bool WithinViewCone(Transform target, Transform me, int halfConeAngle) {
        if(!me) return true;
        if(halfConeAngle >= 180) return true; // 360
        Vector3 dir = target.position - me.position;
        dir.y = 0;
        Vector3 fwd = me.forward;
        fwd.y = 0;
        return Vector3.Angle(fwd, dir) < halfConeAngle;
    }


    public List<List<Vector3>> GetRangeSector(Transform me)
    {
        var groundSectors = GetRangeSector(me.position, me.forward, Vector3.up, 
            minMaxRange.x, minMaxRange.y, viewAngle, 20);
        
        if (rangeType == RangeType.CIRCLE || minMaxHeight.x>=minMaxHeight.y) return groundSectors;
        var filter = this;
        var volumeSectors = groundSectors.Select(s => s.Select(v => new Vector3(v.x, me.position.y + filter.minMaxHeight.x, v.z)).ToList()).ToList();
        volumeSectors.AddRange(groundSectors.Select(s => s.Select(v => new Vector3(v.x, me.position.y + filter.minMaxHeight.y, v.z)).ToList()));
        
        return volumeSectors;
    }
    
    public static List<List<Vector3>> GetRangeSector(Vector3 center, Vector3 middleDirection, Vector3 normal, 
        float innerRadius, float outerRadius, float angle, int segments)
    {
        List<List<Vector3>> sectors = new ();
        if(innerRadius >= outerRadius) return sectors;
        
        List<Vector3> rangeSector = new ();
        
        // Ensure start direction is normalized and perpendicular to normal
        Vector3 right = Vector3.Cross(normal, middleDirection).normalized;
        middleDirection = Vector3.Cross(right, normal).normalized;
        float halfAngle = angle / 2f;
        
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = (float)i / segments * angle;
            Quaternion rotation = Quaternion.AngleAxis(currentAngle - halfAngle, normal);
            Vector3 currentPoint = center + rotation * middleDirection * outerRadius;
            rangeSector.Add(currentPoint);
        }

        if (angle >= 360)
        {
            sectors.Add(rangeSector);
            rangeSector = new List<Vector3>();
        }

        if (innerRadius > 0)
        {
            for (int i = segments; i >=0; i--)
            {
                float currentAngle = (float)i / segments * angle;
                Quaternion rotation = Quaternion.AngleAxis(currentAngle - halfAngle, normal);
                Vector3 currentPoint = center + rotation * middleDirection * innerRadius;
                rangeSector.Add(currentPoint);
            }
        }
        else if (angle <360)
        {
            rangeSector.Add(center); // back to center
        }
        
        sectors.Add(rangeSector);
        
        return sectors;
    }

    public enum RangeType
    {
        CIRCLE, CYLINDER
    }
}