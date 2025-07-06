using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

[CreateAssetMenu(fileName ="BehaviorManager_SO",menuName ="Game Module/Behavior Tree/Behavior Manager")]
public class BehaviorManagerSO : GameModule,IRegistrySO
{
    [RuntimeObject] private readonly Dictionary<Transform,BTMarker> _btDict = new();
    [RuntimeObject] public SensorSystemSO sensorSystem;
    [RuntimeObject] public PlayerStatsSO playerStats;
    [RuntimeObject,SceneRef("PLAYER")] public Transform player;
    
    public BehaviorManagerSO() {
        updateMode = UpdateMode.EVERY_N_FRAME;
    }
    
    public override void OnAwake()
    {
        base.OnAwake();
        sensorSystem = GameRunner.GetSystem<SensorSystemSO>().OrElseThrow(new Exception("SensorSystem not found."));
        playerStats = GameRunner.GetSystem<PlayerStatsSO>().OrElseThrow(new Exception("PlayerStatsSO not found."));
    }
    
    protected override void Update()
    {
        _btDict.Values.Where(bt => bt.Live).ForEach(bt => bt.tree.Run());
    }

    #region Registry
    public void Register(Transform tf)
    {
        if(!tf.TryGetComponent<BTMarker>(out var marker)) return;
        var tree = marker.tree.Clone();
        tree.Me = tf;
        tree.Initialize();
        marker.tree = tree;
        _btDict.Add(tf, marker);
    }

    public void Unregister(Transform tf)
    {
        _btDict.Remove(tf);
    }
    #endregion 
}
