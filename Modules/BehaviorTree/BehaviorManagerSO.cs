using System.Collections.Generic;
using System.Linq;
using ModularFramework;
using UnityEngine;

[CreateAssetMenu(fileName ="BehaviorManager_SO",menuName ="Game Module/Behavior Tree/Behavior Manager")]
public class BehaviorManagerSO : GameModule<BehaviorManagerSO>,IRegistrySO
{
    [RuntimeObject] private readonly Dictionary<Transform,BTRunner> _btDict = new();
    [RuntimeObject] public Autowire<SensorSystemSO> sensorSystem;
    [SceneRef("PLAYER")] public Transform player;
    
    public BehaviorManagerSO() {
        updateMode = UpdateMode.EVERY_N_FRAME;
    }

    protected override void OnAwake() { }

    protected override void OnStart() { }

    protected override void OnUpdate()
    {
        _btDict.Values.Where(bt => bt.Live).ForEach(bt => bt.tree.Run());
    }
    
    protected override void OnSceneDestroy() { }

    protected override void OnDraw() { }

    #region Registry
    public void Register(Transform tf)
    {
        if(!tf.TryGetComponent<BTRunner>(out var marker)) return;
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
