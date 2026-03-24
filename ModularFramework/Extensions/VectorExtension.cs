using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModularFramework
{
    public static class VectorExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(this Vector3 v) => v is { x: > 0, y: > 0, z: > 0 };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(this Vector3 v) => v is { x: < 0, y: < 0, z: < 0 };
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IgnoreX(this Vector3 v) => new(0, v.y, v.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IgnoreY(this Vector3 v) => new(v.x, 0, v.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 IgnoreZ(this Vector3 v) => new(v.x, v.y, 0);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SqrMagnitudeWithoutX(this Vector3 v) 
            => (float) ((double)v.y * v.y + (double)v.z * v.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SqrMagnitudeWithoutY(this Vector3 v) 
            => (float) ((double)v.x * v.x + (double)v.z * v.z);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SqrMagnitudeWithoutZ(this Vector3 v) 
            => (float) ((double)v.x * v.x + (double)v.y * v.y);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AddX(this Vector3 v, float x)
        {
            return new Vector3(v.x + x, v.y, v.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AddY(this Vector3 v, float y)
        {
            return new Vector3(v.x, v.y + y, v.z);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AddZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, v.z + z);
        }
    }
}