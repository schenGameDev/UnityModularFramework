using EditorAttributes;
using Unity.Mathematics;
using UnityEngine;
using ModularFramework.Utility;

public class LockTargetCamera : MovingCameraBase
{
    // Player forward direction is target, Player locate at left side of screen

    [SerializeField] Transform _player;


    [FoldoutGroup("Player",nameof(_playerOffCenter), nameof(_rightSide),nameof(_focusRadius))]
    [SerializeField] private Void playerPlaceGroup;

    [FoldoutGroup("Camera Height",nameof(_camHeightCurve),nameof(_camBackwardCurve),nameof(_offCenterCurve),nameof(_minViewAngleOutOfDistance))]
    [SerializeField] private Void cameraHeightGroup;

    [HideInInspector,SerializeField] private AnimationCurve _camHeightCurve;
    [HideInInspector,SerializeField] private AnimationCurve _camBackwardCurve;
    [HideInInspector,SerializeField,Suffix("[0,1]")] private AnimationCurve _offCenterCurve;
    [HideInInspector,SerializeField,Rename("View Angle Low Limit X Outside Y m"), Suffix("(Horizon 0 deg)")] private Vector2 _minViewAngleOutOfDistance;


    [FoldoutGroup("Closeup",nameof(_closeUpDistance),nameof(_closeUpNoRollAngle))]
    [SerializeField] private Void cameraCloseupGroup;

    [HideInInspector,SerializeField] private float _playerOffCenter=2;
    [HideInInspector,SerializeField,ReadOnly] private bool _rightSide;

    [HideInInspector,SerializeField,Rename("Don't Switch Side Within"),Suffix("m")]  private float _closeUpDistance = 3f;
    [HideInInspector,SerializeField,Rename("Roll Slow Within"),Suffix("deg")]  private float _closeUpNoRollAngle = 5;



    [HideInInspector,SerializeField,Range(0,1)] private float _focusRadius=0.3f;
    private LockManagerSO _lockManager;

    public LockTargetCamera() {
        Type = CameraType.LOCK;
        cameraOffSet = new(0,0,-3);
        registryTypes = new[] {(typeof(CameraManagerSO), 1) , (typeof(LockManagerSO), 2)};
    }

    protected override void Start()
    {
        base.Start();
        _lockManager = GetRegistry<LockManagerSO>().Get();
    }

    protected override void Update()
    {
        base.Update();
        if(isLive) {
            MoveCamera();
            if(_playerOffCenter==0) RollCamera();
            else {
                if(_isMovingToCenter) {
                    RollCamera();
                    _isMovingToCenter = !IsTargetInScreenCenter();
                } else {
                    _isMovingToCenter = !IsTargetInFocusRadius();
                }
            }
        }
    }

    [ReadOnly,SerializeField] private bool _isMovingToCenter = true;
    public bool LostTarget() => _lockManager.LockTarget==null;
    private bool IsTargetInFocusRadius() => IsTargetInScreenRadius(_focusRadius);
    private bool IsTargetInScreenCenter() => IsTargetInScreenRadius(0.05f);

    private bool IsTargetInScreenRadius(float radius) {
        if(LostTarget()) return false;
        Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(_lockManager.LockTarget.position);
        float maxR = math.min(Screen.width / 2, Screen.height / 2);
        float offCenterDistanceSqr = Vector2.SqrMagnitude(targetScreenPos - new Vector2(Screen.width / 2, Screen.height / 2));
        return offCenterDistanceSqr <  math.pow(radius * maxR,2);
    }

    private void MoveCamera() {
        if(LostTarget()) return;

        if(delayTimer>0) {
            FocusPointDecelerate();
            delayTimer -= Time.deltaTime;
            return;
        }

        var targetToPlayer = _lockManager.LockTarget.position - _player.position;
        var dist = targetToPlayer.magnitude;
        var camHeight = GetCameraHeight(dist);

        float centerDist = 1;
        bool isCenter = dist < centerDist;

        bool noOffCenter = _playerOffCenter == 0;

        Vector3 focusRelativeToPlayer = Vector3.zero;
        if(!isCenter && !noOffCenter) {
            float offCenterDist = _playerOffCenter * _offCenterCurve.Evaluate(dist);
            var targetToPlayerXZ = new Vector3(targetToPlayer.x, targetToPlayer.y - 1, targetToPlayer.z);
            focusRelativeToPlayer = Vector3.Cross(targetToPlayer,targetToPlayerXZ).normalized * offCenterDist;
        }

        Vector3 camTarget;
        if(noOffCenter) {
            camTarget = GetFocusPointLocation(focusRelativeToPlayer,camHeight);
            if(Vector3.SqrMagnitude(focusPoint.position-camTarget) <= 0.001f) {
                delayTimer = followDelay;
                FocusPointDecelerate();
                return;
            }

        } else {
            var leftCamTarget = GetFocusPointLocation(focusRelativeToPlayer,camHeight);
            var rightCamTarget = GetFocusPointLocation(-focusRelativeToPlayer,camHeight);

            if(Vector3.SqrMagnitude(focusPoint.position-leftCamTarget) <= 0.001f || Vector3.SqrMagnitude(focusPoint.position-rightCamTarget) <= 0.001f ) {
                delayTimer = followDelay;
                FocusPointDecelerate();
                return;
            }

            var distLeft = Vector3.SqrMagnitude(transform.position - leftCamTarget);
            var distRight = Vector3.SqrMagnitude(transform.position - rightCamTarget);


            float switchSideTolerance = 0.5f;

            if(!_rightSide && distLeft - distRight > switchSideTolerance) {
                _rightSide = true;
            } else if(_rightSide && distRight - distLeft > switchSideTolerance) {
                _rightSide = false;
            }

            camTarget = _rightSide ? rightCamTarget : leftCamTarget;
        }

        if(FocusPointChase(camTarget)) {
            delayTimer = followDelay;
        }

    }

    Vector3 _currentOffset;
    private float GetCameraHeight(float dist)
    {
        var camHeight = _camHeightCurve.Evaluate(dist);
        UpdateCurrentOffset(dist);
        SetFollowOffset(_currentOffset);
        return camHeight;
    }

    private void UpdateCurrentOffset(float dist) {
        _currentOffset = cameraOffSet - new Vector3(0,0, _camBackwardCurve.Evaluate(dist));
    }

    public override Vector3 Offset => _currentOffset;

    private Vector3 GetFocusPointLocation(Vector3 focusRelativeToPlayer, float camHeight) {
        return new Vector3(_player.position.x + focusRelativeToPlayer.x, _player.position.y + camHeight,
                           _player.position.z + focusRelativeToPlayer.z);
    }

    private void RollCamera() {
        if(_lockManager.LockTarget==null) return;
        var sqrDist = (_lockManager.LockTarget.position - _player.position).sqrMagnitude;
        var dir = _lockManager.LockTarget.position - focusPoint.position;

        if(sqrDist > _minViewAngleOutOfDistance.y * _minViewAngleOutOfDistance.y &&
           dir.y < 0) {
            //out of range
            dir.y = 0;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);

        var roll =Quaternion.RotateTowards(focusPoint.rotation,targetRot,rollSpeed * Time.deltaTime);
        focusPoint.eulerAngles = new Vector3(roll.eulerAngles.x, roll.eulerAngles.y, 0);

        if(Quaternion.Angle(focusPoint.rotation, targetRot) > _closeUpNoRollAngle) {
            RollAccelerate();
        } else {
            rollSpeed = rollStartSpeed;
        }
    }

    public override void OnEnter(CameraTransitionType transitionType) {
        base.OnEnter(transitionType);
        var targetToPlayer = _lockManager.LockTarget.position - _player.position;
        UpdateCurrentOffset(targetToPlayer.magnitude);
        if(transitionType != CameraTransitionType.NONE) MatchPrevCamPosition();
        if(transitionType == CameraTransitionType.MATCH_LAST_ROT) {
            DebugUtil.Warn("Unsupported transition type " + CameraTransitionType.MATCH_LAST_ROT);
        }
        _isMovingToCenter = true;
    }

    protected override Transform CameraFocusSpawnPoint() => _player;

    public bool LockTargetExist() => _lockManager.LockTargetExist();

}
