using ModularFramework;
using UnityEngine;

[AddComponentMenu("NPC/Monster", 0), DisallowMultipleComponent]
public class Monster : Npc
{
    protected virtual void OnEnable()
    {
        Registry<Monster>.TryAdd(this);
    }

    protected virtual void OnDisable()
    {
        Registry<Monster>.Remove(this);
    }
    
    protected virtual void OnDestroy()
    {
        Registry<Monster>.Remove(this);
    }
}