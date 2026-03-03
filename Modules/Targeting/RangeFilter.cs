using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.Targeting
{
    [Serializable]
    public struct RangeFilter : ITransformTargetFilter
    {
        public RangeType rangeType;
        [Clamp(0, 999, 0, 999)] public Vector2 minMaxRange;
        [ShowField(nameof(IsAngular)),Range(0, 360)] 
        public int viewAngle;
        
        [ShowField(nameof(IsRectangular)),Min(0)] 
        public float width;

        [ShowField(nameof(IsVolumetric))]
        public Vector2 minMaxHeight;


        /// <summary>
        /// when me is null, ignore view cone
        /// </summary>
        /// <param name="target"></param>
        /// <param name="me"></param>
        /// <returns></returns>
        public bool IsIncluded(Transform target, Transform me = null)
        {
            if(!WithinGroundRange(target, me, minMaxRange)) return false;
            if(rangeType is RangeType.CIRCLE or RangeType.CYLINDER && 
               !WithinViewCone(target, me, viewAngle / 2)) return false;
            if (rangeType is RangeType.SQUARE or RangeType.BOX &&
                !WithinWidth(target, me, width / 2)) return false;
            if (rangeType is RangeType.BOX or RangeType.CYLINDER && 
                minMaxHeight.x<=minMaxHeight.y && 
                !WithinHeightRange(target, me, minMaxHeight)) return false;
            return true;
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(rangeType, minMaxRange, viewAngle, width, minMaxHeight);
        }

        public static bool WithinGroundRange(Transform a, Transform b, Vector2 minMaxRange)
        {
            if (!a || !b) return false;
            var diff = a.position - b.position;
            diff.y = 0;
            var sqrDist = Vector3.SqrMagnitude(diff);
            return sqrDist <= minMaxRange.y * minMaxRange.y &&
                   sqrDist >= minMaxRange.x * minMaxRange.x;
        }

        public static bool WithinHeightRange(Transform target, Transform me, Vector2 minMaxHeight)
        {
            if (!target || !me) return false;
            var heightDiff = target.position.y - me.position.y;
            return heightDiff <= minMaxHeight.y &&
                   heightDiff >= minMaxHeight.x;
        }

        public static bool WithinViewCone(Transform target, Transform me, int halfConeAngle)
        {
            if (!me) return true;
            if (halfConeAngle >= 180) return true; // 360
            Vector3 dir = target.position - me.position;
            dir.y = 0;
            Vector3 fwd = me.TryGetComponent<IDelayFacing>(out var runner) 
                ? runner.TargetFacingDirection.IgnoreY() 
                : me.forward.IgnoreY();
            return Vector3.Angle(fwd, dir) < halfConeAngle;
        }
        
        public static bool WithinWidth(Transform target, Transform me, float halfWidth)
        {
            Vector3 fwd = me.TryGetComponent<IDelayFacing>(out var runner) 
                ? runner.TargetFacingDirection.IgnoreY() 
                : me.forward.IgnoreY();
            fwd.Normalize();
        
            Vector3 toTarget = target.position - me.position;
            toTarget.y = 0;
        
            // Calculate perpendicular distance from target to forward direction
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            float perpDistance = Mathf.Abs(Vector3.Dot(toTarget, right));
        
            return perpDistance <= halfWidth;
        }

        public List<List<Vector3>> GetRangeSector(Transform me)
        {
            Vector3 fwd = Application.isPlaying && me.TryGetComponent<IDelayFacing>(out var runner)
                ? runner.TargetFacingDirection
                : me.forward;
            var groundSectors = rangeType is RangeType.SQUARE or RangeType.BOX
                ? GetSquareRangeSector(me, fwd, Vector3.up,minMaxRange.x, minMaxRange.y, width)
                : GetCircleRangeSector(me.position, fwd, Vector3.up, 
                    minMaxRange.x, minMaxRange.y, viewAngle, 20);
        
            if (rangeType is RangeType.CIRCLE or RangeType.SQUARE || minMaxHeight.x>=minMaxHeight.y) return groundSectors;
            var filter = this;
            var volumeSectors = groundSectors.Select(s =>
                s.Select(v => new Vector3(v.x, me.position.y + filter.minMaxHeight.x, v.z)).ToList()).ToList();
            volumeSectors.AddRange(groundSectors.Select(s =>
                s.Select(v => new Vector3(v.x, me.position.y + filter.minMaxHeight.y, v.z)).ToList()));

            return volumeSectors;
        }

        private static List<List<Vector3>> GetCircleRangeSector(Vector3 center, Vector3 middleDirection, Vector3 normal,
            float innerRadius, float outerRadius, float angle, int segments)
        {
            List<List<Vector3>> sectors = new();
            if (innerRadius >= outerRadius) return sectors;

            List<Vector3> rangeSector = new();

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
                for (int i = segments; i >= 0; i--)
                {
                    float currentAngle = (float)i / segments * angle;
                    Quaternion rotation = Quaternion.AngleAxis(currentAngle - halfAngle, normal);
                    Vector3 currentPoint = center + rotation * middleDirection * innerRadius;
                    rangeSector.Add(currentPoint);
                }
            }
            else if (angle < 360)
            {
                rangeSector.Add(center); // back to center
            }

            sectors.Add(rangeSector);

            return sectors;
        }
        
        private static List<List<Vector3>> GetSquareRangeSector(Transform center, Vector3 middleDirection, Vector3 normal, 
            float innerRadius, float outerRadius, float width)
        {
            List<List<Vector3>> sectors = new ();
            if(innerRadius >= outerRadius) return sectors;
            List<Vector3> rangeSector = new ();
        
            // Normalize directions
            Vector3 right = Vector3.Cross(normal, middleDirection).normalized;
            middleDirection = Vector3.Cross(right, normal).normalized;
        
            float halfWidth = width / 2f;
            Vector3 centerPos = center.position;
        
            // Outer box corners (clockwise from front-right)
            rangeSector.Add(centerPos + middleDirection * outerRadius + right * halfWidth);  // Front-right
            rangeSector.Add(centerPos + middleDirection * outerRadius - right * halfWidth);  // Front-left
        
            if (innerRadius > 0)
            {
                // Inner box corners (counter-clockwise from front-left to back-left)
                rangeSector.Add(centerPos + middleDirection * innerRadius - right * halfWidth);  // Inner front-left
                rangeSector.Add(centerPos + middleDirection * innerRadius + right * halfWidth);  // Inner front-right
            }
            else
            {
                // If no inner radius, close the box at the center
                rangeSector.Add(centerPos - right * halfWidth);  // Back-left
                rangeSector.Add(centerPos + right * halfWidth);  // Back-right
            }
        
            sectors.Add(rangeSector);
            return sectors;
        }
        
        private bool IsAngular => rangeType is RangeType.CIRCLE or RangeType.CYLINDER;
        private bool IsRectangular => rangeType is RangeType.SQUARE or RangeType.BOX;
        private bool IsVolumetric => rangeType is RangeType.CYLINDER or RangeType.BOX;

        public enum RangeType : byte
        {
            CIRCLE,
            CYLINDER,
            SQUARE,
            BOX
        }
    }
}