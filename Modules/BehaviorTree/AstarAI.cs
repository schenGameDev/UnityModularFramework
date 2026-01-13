using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
using Pathfinding;
using UnityEngine;

public class AstarAI : MonoBehaviour
{
    [Header("Config")]
    private float _nextWaypointDistance = 1;
    private float _repathRate = 0.5f;
    [SerializeField]private float _stoppingDistance = 1f;
    [SerializeField] private EventChannel<float> _npcSpeedChangeEvent;

    [Header("Runtime")]
    [ReadOnly,SerializeField] private bool _targetFixed;
    [ReadOnly,SerializeField,HideField(nameof(_targetFixed))] private Transform _target;
    [ReadOnly,SerializeField,ShowField(nameof(_targetFixed)),Rename("Target")] private Vector3 _targetPos;
    [ReadOnly,SerializeField] private float _speed;
    [ReadOnly,SerializeField] private bool _slowDownAtEnd;

    public bool FixedTargetReached {get; private set;}
    public bool PathNotFound {get; private set;}
    private Seeker _seeker;
    private CharacterController _controller;
    private BTMarker _runner;

    private int _currentWaypoint;
    private Path _path;
    private float _lastRepath = float.NegativeInfinity;
    private float _speedModifier = 1;


    private void Awake() {
        _seeker = GetComponent<Seeker>();
        _controller = GetComponent<CharacterController>();
        _runner = GetComponent<BTMarker>();
    }

    private void OnEnable() {
        _npcSpeedChangeEvent.AddListener(SetSpeedModifier);
        if(_npcSpeedChangeEvent.TryRequest(out float x))
            _speedModifier = x;
        else _speedModifier = 1;
    }

    private void OnDisable() {
        _npcSpeedChangeEvent.RemoveListener(SetSpeedModifier);
    }

    private void SetSpeedModifier(float modifier) {
        _speedModifier = modifier;
    }

    public void Teleport(Vector3 target) {
        _controller.enabled = false;
        transform.position = PhysicsUtil.FindGroundPosition(target);
        _controller.enabled = true;
    }

    public void Stop() {
        Reset();
        _speed = 0;
        _controller.Move(Vector3.zero);
    }

    public void Reset() {
        _target = null;
        _targetFixed = false;
        FixedTargetReached = false;
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
        PathNotFound = false;
        _seeker.StartPath(transform.position, _target.position, OnPathComplete);
    }

    public void SetNewTarget(Vector3 point, float speed, bool slowDownAtEnd) {
        _targetFixed = true;
        FixedTargetReached = false;
        PathNotFound = false;
        _speed = speed;
        _targetPos = point;
        _target = null;
        _slowDownAtEnd = slowDownAtEnd;
        _seeker.StartPath(transform.position, _targetPos, OnPathComplete);
    }

    public void SetNewTargetUnFixed(Vector3 point, float speed, bool slowDownAtEnd) {
        _targetFixed = false;
        PathNotFound = false;
        _speed = speed;
        _targetPos = point;
        _target = null;
        _slowDownAtEnd = slowDownAtEnd;
        _seeker.StartPath(transform.position, _targetPos, OnPathComplete);
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
        if (Time.time > _lastRepath + _repathRate && _seeker.IsDone() && Vector3.SqrMagnitude(transform.position - pos) > 0.01f) {
            _lastRepath = Time.time;
            _seeker.StartPath(transform.position, pos, OnPathComplete);
        }



    }

    private void Update () {
        if(SingletonRegistry<GameRunner>.TryGet(out var runner) && runner.IsPause) return;
        RecalculatePath();

        if (_path == null) {
            if(!_controller.isGrounded) _controller.SimpleMove(Vector3.down);
            return;
        }

        bool reachedEndOfPath = false;
        // The distance to the next waypoint in the path
        float sqrDistanceToWaypoint;
        while (true) {
            sqrDistanceToWaypoint = Vector3.SqrMagnitude(transform.position - _path.vectorPath[_currentWaypoint]);
            if (sqrDistanceToWaypoint < _nextWaypointDistance) {
                if (_currentWaypoint + 1 < _path.vectorPath.Count) {
                    _currentWaypoint++;
                } else {
                    if(sqrDistanceToWaypoint < 0.01f) {
                        _path.Release(this);
                        _path = null;
                    }
                    if(_targetFixed) FixedTargetReached = true;
                    reachedEndOfPath = true;
                    break;
                }
            } else {
                break;
            }
        }

        // Slow down smoothly upon approaching the end of the path
        // This value will smoothly go from 1 to 0 as the agent approaches the last waypoint in the path.
        var speedFactor = _slowDownAtEnd && reachedEndOfPath ? Mathf.Sqrt(Mathf.Sqrt(sqrDistanceToWaypoint)/_nextWaypointDistance) : 1f;
        if(_path==null) return;
        Vector3 dir = (_path.vectorPath[_currentWaypoint] - transform.position).normalized;
        _runner.faceDirection = dir;
        _velocity = _speed==0? Vector3.zero : _speedModifier * _speed * speedFactor * dir;


    }
    private Vector3 _velocity;
    private void FixedUpdate() {
        if (_path == null) {
            return;
        }
        _controller.SimpleMove(_velocity);
    }
}
