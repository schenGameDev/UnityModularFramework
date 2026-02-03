using EditorAttributes;
using ModularFramework;
using Sisus.ComponentNames;
using UnityEngine;
using UnityTimer;

[AddComponentMenu("Behavior Tree/Move", 0), RequireComponent(typeof(BTRunner))]
public class BTMove : MonoBehaviour,IUniqueIdentifiable, IReady
{
    public string UniqueId => moveName;

    [SerializeField,OnValueChanged(nameof(RenameComponent))] private string moveName;
    public float speed;
    [SerializeField] private string animFlag;
    [SerializeField] private string pathName;
    
    private CountdownTimer _cooldownTimer;
    private BTRunner _runner;
    private WaypointCollection _waypointCollection;
    [ShowInInspector, ReadOnly] private bool _running;
    [ReadOnly,ShowInInspector] protected bool isReady = true;
    public bool Ready => isReady;
    
    private void RenameComponent() => this.SetName($"Move: {moveName}");
    
    private void Awake()
    {
        _runner = GetComponent<BTRunner>();
        _waypointCollection = GetComponent<WaypointCollection>();
    }
    
    public Vector3[] Path => string.IsNullOrEmpty(pathName)|| !_waypointCollection 
        ? null : _waypointCollection?.GetPath(pathName);


    public void Move()
    {
        if(_running) return;
        _runner.PlayAnim(animFlag);
        _running = true;
    }

    public void Stop()
    {
        if(!_running) return;
        _runner.StopAnim(animFlag);
        _running = false;
    }
}