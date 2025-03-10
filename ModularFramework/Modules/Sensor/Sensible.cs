using System;
using UnityEngine;
using ModularFramework;
/// <summary>
/// Class <c>Sensible</c> marks a gameobject to be constantly monitored by sensor Manager during gameplay
/// </summary>
///
[DisallowMultipleComponent]
public class Sensible : Marker {
    [Header("Config")]
    public bool IsVisible; // player insensible to certain monster, force out of invisible if touched


    public Sensible()
    {
        registryTypes = new[] { (typeof(SensorManagerSO),1)};
    }
}
