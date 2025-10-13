using UnityEngine;

namespace ModularFramework
{
    public static class ColliderExtension
    {
        static readonly Collider[] overlapCache = new Collider[32];

        /// <summary>
        /// Check for collision with objects in the provided layer and Compute total correction vector
        /// </summary>
        /// <param name="source"></param>
        /// <param name="layerMask"></param>
        /// <param name="totalCorrection"></param>
        /// <returns></returns>
        public static bool GetPenetrationInLayer(this Collider source, LayerMask layerMask, out Vector3 totalCorrection)
        {
            totalCorrection = Vector3.zero;
            if(source == null) return false;
            
            int count = Physics.OverlapBoxNonAlloc(
                source.bounds.center,
                source.bounds.extents,
                overlapCache,
                source.transform.rotation,
                layerMask);
            
            bool collided = false;
            for (int i = 0; i < count; i++)
            {
                Collider collider = overlapCache[i];
                if(collider == source) continue;

                if (source.ComputePenetration(collider, out Vector3 direction, out float distance))
                {
                    totalCorrection += direction * distance;
                    collided = true;
                }
            }
            
            return collided;
        }

        public static bool ComputePenetration(this Collider source, Collider target, out Vector3 direction,
            out float distance)
        {
            direction = Vector3.zero;
            distance = 0;
            
            if(!source || !target) return false;

            return Physics.ComputePenetration(
                source, source.transform.position, source.transform.rotation,
                target, target.transform.position, target.transform.rotation,
                out direction, out distance);
        }
    }
}
