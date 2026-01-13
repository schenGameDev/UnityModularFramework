using System.Collections.Generic;
using UnityEngine;

public interface IPositionCalculator
{
    Vector3 GetPosition(Vector3 origin, IEnumerable<Vector3> targets);
}