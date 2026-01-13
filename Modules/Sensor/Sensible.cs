using ModularFramework;
using ModularFramework.Utility;
using UnityEngine;

/// <summary>
/// Class <c>Sensible</c> marks a gameobject to be constantly monitored by sensor Manager during gameplay
/// </summary>
///
[DisallowMultipleComponent]
public class Sensible : Marker {
    [Header("Config")]
    public bool isVisible; // player insensible to certain monster, force out of invisible if touched


    public override void RegisterAll()
    {
        SingletonRegistry<SensorSystemSO>.Instance?.Register(transform);
    }

    protected override void UnregisterAll()
    {
        SingletonRegistry<SensorSystemSO>.Instance?.Unregister(transform);
    }
}
