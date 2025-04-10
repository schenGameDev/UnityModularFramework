using System;
using ModularFramework;
using UnityEngine;
using UnityEngine.Serialization;

public class BTMarker : Marker,ILive
{
    public BehaviorTreeSO tree;
    public Vector3 faceDirection;

    public BTMarker()
    {
        RegistryTypes = new[] { new[] { typeof(BehaviorManagerSO) } };
    }
    

    public void PlayAnim(string flags) {

    }

    public bool Live { get; set; }
}
