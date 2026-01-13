using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct RandomSelector : ITransformTargetSelector
{
    public float GetScore(Transform target, Transform me)
    {
        return Random.value;
    }
}