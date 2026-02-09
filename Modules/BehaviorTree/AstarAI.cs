using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Utility;
using Pathfinding;
using UnityEngine;

public class AstarAI : MonoBehaviour
{
    [Header("Config")]
    private const float NEXT_WAYPOINT_DISTANCE = 1;
    private const float REPATH_RATE = 0.5f;
    // [SerializeField] private float stoppingDistance = 1f;
    [SerializeField] private float heightOffset = 1f;
    [SerializeField] private EventChannel<float> npcSpeedChangeEvent;
    [Tooltip("the customized tags in seeker")] public uint[] astarTags;


    [Header("Runtime")]
    [ReadOnly,ShowInInspector] private bool _targetFixed;
    [ReadOnly,ShowInInspector,HideField(nameof(_targetFixed))] private Transform _target;
    [ReadOnly,ShowInInspector,ShowField(nameof(_targetFixed)),Rename("Target")] private Vector3 _targetPos;
    [ReadOnly,ShowInInspector] private float _speed;
    [ReadOnly,ShowInInspector] private bool _slowDownAtEnd;

    public bool TargetReached {get; private set;}
    public bool PathNotFound {get; private set;}
    [SerializeField,Self] private Seeker seeker;
    [SerializeField,Self] private CharacterController controller;
    [SerializeField,Self] private BTRunner runner;

    private int _currentWaypoint;
    private Path _path;
    private float _lastRepath = float.NegativeInfinity;
    private float _speedModifier = 1;

#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif

    private void OnEnable() {
        npcSpeedChangeEvent.AddListener(SetSpeedModifier);
        if(npcSpeedChangeEvent.TryRequest(out float x))
            _speedModifier = x;
        else _speedModifier = 1;
    }
    
    private void OnDisable() {
        npcSpeedChangeEvent.RemoveListener(SetSpeedModifier);
    }

    private void SetSpeedModifier(float modifier) {
        _speedModifier = modifier;
    }

    public void Teleport(Vector3 target) {
        controller.enabled = false;
        transform.position = PhysicsUtil.FindGroundPosition(target);
        controller.enabled = true;
    }

    public void Stop() {
        Reset();
        _speed = 0;
        controller.Move(Vector3.zero);
    }

    public void Reset() {
        _target = null;
        _targetFixed = false;
        TargetReached = false;
        if(_path!=null) {
            _path.Release(this);
            _path = null;
        }

    }

    public void SetNewTarget(Transform tf, float speed, bool slowDownAtEnd) {
        _targetFixed = false;
        _speed = speed;
        _target = tf;
        _slowDownAtEnd = slowDownAtEnd;
        TargetReached = false;
        PathNotFound = false;
        seeker.StartPath(transform.position, _target.position, OnPathComplete);
    }

    public void SetNewTarget(Vector3 point, float speed, bool slowDownAtEnd) {
        _targetFixed = true;
        TargetReached = false;
        PathNotFound = false;
        _speed = speed;
        _targetPos = point;
        _target = null;
        _slowDownAtEnd = slowDownAtEnd;
        seeker.StartPath(GetTransformGroundPos(), _targetPos, OnPathComplete);
    }

    public void SetNewTargetUnFixed(Vector3 point, float speed, bool slowDownAtEnd) {
        _targetFixed = false;
        PathNotFound = false;
        TargetReached = false;
        _speed = speed;
        _targetPos = point;
        _target = null;
        _slowDownAtEnd = slowDownAtEnd;
        seeker.StartPath(GetTransformGroundPos(), _targetPos, OnPathComplete);
    }

    public void UpdateTarget(Vector3 point) {
        _targetPos = point;
    }

    private void OnPathComplete(Path p) {
        p.Claim(this);
        if (!p.error) {
            if (_path != null) _path.Release(this);
            _path = p;
            // Reset the waypoint counter so that we start to move towards the first point in the path
            _currentWaypoint = 0;
        } else {
            PathNotFound = true;
            p.Release(this);
        }
    }

    private void RecalculatePath() {
        if(_targetFixed) return;
        var pos = _target == null? _targetPos : _target.position;
        var tfGroundPos = GetTransformGroundPos();
        if (Time.time > _lastRepath + REPATH_RATE && seeker.IsDone() && Vector3.SqrMagnitude(tfGroundPos - pos) > 0.01f) {
            _lastRepath = Time.time;
            seeker.StartPath(tfGroundPos, pos, OnPathComplete);
        }
    }

    private void Update () {
        if(SingletonRegistry<GameRunner>.TryGet(out var runner) && runner.IsPause) {return;}
        RecalculatePath();

        if (_path == null) {
            if(!controller.isGrounded) controller.SimpleMove(Vector3.down);
            return;
        }

        bool reachedEndOfPath = false;
        // The distance to the next waypoint in the path
        float sqrDistanceToWaypoint = 0;
        var tfGroundPos = GetTransformGroundPos();
        int i = 0;
        const int maxIterations = 100; // safety to prevent infinite loop
        while (i++ <= maxIterations) {
            sqrDistanceToWaypoint = Vector3.SqrMagnitude(tfGroundPos - _path.vectorPath[_currentWaypoint]);
            if (sqrDistanceToWaypoint < NEXT_WAYPOINT_DISTANCE) {
                if (_currentWaypoint + 1 < _path.vectorPath.Count) {
                    _currentWaypoint++;
                } else {
                    if(sqrDistanceToWaypoint < 0.01f) {
                        _path.Release(this);
                        _path = null;
                    }
                    TargetReached = true;
                    reachedEndOfPath = true;
                    break;
                }
            } else {
                break;
            }
        }

        // Slow down smoothly upon approaching the end of the path
        // This value will smoothly go from 1 to 0 as the agent approaches the last waypoint in the path.
        var speedFactor = _slowDownAtEnd && reachedEndOfPath ? Mathf.Sqrt(Mathf.Sqrt(sqrDistanceToWaypoint)/NEXT_WAYPOINT_DISTANCE) : 1f;
        if(_path==null) return;
        Vector3 dir = (_path.vectorPath[_currentWaypoint] - tfGroundPos).normalized;
        if(dir != Vector3.zero && _speed>0) this.runner.FaceDirection(dir);
        _velocity = _speed==0? Vector3.zero : _speedModifier * _speed * speedFactor * dir;


    }
    private Vector3 _velocity;
    private float _stuckTimer = 0f;
    private void FixedUpdate() {
        if (_path == null) {
            return;
        }

        if (!controller.SimpleMove(_velocity) || (_velocity.sqrMagnitude>0.01f && controller.velocity is { x: 0, z: 0 }))
        {
            _stuckTimer += Time.fixedDeltaTime;
            if (!PathNotFound && _stuckTimer > 1f)
            {
                Debug.Log($"{name} stuck.");
                PathNotFound = true;
                seeker.CancelCurrentPathRequest();
            }
            else
            {
                _stuckTimer = 0;
            }
        }
        
    }

    private Vector3 GetTransformGroundPos()
    {
        return transform.position - new Vector3(0,heightOffset,0);
    }
}
