using System;
using UnityEngine;
using ModularFramework;
using UnityEngine.Serialization;

/// <summary>
/// Class <c>Sensible</c> marks a gameobject to be constantly monitored by sensor Manager during gameplay
/// </summary>
///
[DisallowMultipleComponent]
public class Sensible : Marker {
    [Header("Config")]
    public bool isVisible; // player insensible to certain monster, force out of invisible if touched


    public Sensible()
    {
        registryTypes = new[] { (typeof(SensorManagerSO),1)};
    }
}
