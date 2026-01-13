using UnityEngine;

public abstract class Character : MonoBehaviour
{
    public int Health { get; }
    public int Dps { get; }
    public bool IsFriendly { get; }
}