using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ModularFramework.Modules.Targeting
{
    [Serializable]
    public struct RandomSelector : ITransformTargetSelector
    {
        public bool SkipNegativeScore => false;

        public float GetScore(Transform target, Transform me)
        {
            return Random.value;
        }
    }
}