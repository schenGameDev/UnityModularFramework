using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    public interface IPositionCalculator
    {
        Vector3 GetPosition(Vector3 origin, IEnumerable<Vector3> targets);
    }
}