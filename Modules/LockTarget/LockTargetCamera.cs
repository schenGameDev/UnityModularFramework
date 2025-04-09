using System;
using EditorAttributes;
using ModularFramework;
using Unity.Mathematics;
using UnityEngine;
using ModularFramework.Utility;
using Void = EditorAttributes.Void;

public class LockTargetCamera : MovingCameraBase
{
    // Player forward direction is target, Player locate at left side of screen

    [SerializeField] Transform player;

    
    [FoldoutGroup("Player",nameof(playerOffCenter), nameof(rightSide),nameof(focusRadius), nameof(fasterWhenTargetAtScreenEdge), nameof(speedBoostExponential))]
    [SerializeField] private Void playerPlaceGroup;

    [FoldoutGroup("Camera Height",nameof(camHeightCurve),nameof(camBackwardCurve),nameof(offCenterCurve),nameof(minViewAngleOutOfDistance))]
    [SerializeField] private Void cameraHeightGroup;

    [HideInInspector,SerializeField] private AnimationCurve camHeightCurve;
    [HideInInspector,SerializeField] private AnimationCurve camBackwardCurve;
    [HideInInspector,SerializeField,Suffix("[0,1]")] private AnimationCurve offCenterCurve;
    [HideInInspector,SerializeField,Rename("View Angle Low Limit X Outside Y m"), Suffix("(Horizon 0 deg)")] private Vector2 minViewAngleOutOfDistance;


    [FoldoutGroup("Closeup",nameof(closeUpNoRollAngle))]
    [SerializeField] private Void cameraCloseupGroup;

    [HideInInspector,SerializeField] private float playerOffCenter=2;
    [HideInInspector,SerializeField] private bool fasterWhenTargetAtScreenEdge;
    [HideInInspector,SerializeField, Range(1,4),Rename("Roll Acc ^X"), ShowField(nameof( fasterWhenTargetAtScreenEdge))] 
    private int speedBoostExponential = 3;
    [HideInInspector,SerializeField,ReadOnly] private bool rightSide;

    // [HideInInspector,SerializeField,Rename("Don't Switch Side Within"),Suffix("m")]  private float _closeUpDistance = 3f;
    [HideInInspector,SerializeField,Rename("Roll Slow Within"),Suffix("deg")]  private float closeUpNoRollAngle = 5;

    [HideInInspector,SerializeField,Range(0,1)] private float focusRadius=0.3f;
    private LockManagerSO _lockManager;
    
    public override Type[][] RegistryTypes => new[] {new[]{typeof(CameraManagerSO)} , new[]{typeof(LockManagerSO)}};

    public LockTargetCamera() {
        type = CameraType.LOCK;
        cameraOffSet = new(0,0,-3);
    }

    protected override void Start()
    {
        base.Start();
        _lockManager = GetComponent<Marker>().GetRegistry<LockManagerSO>().Get();
    }

    protected override void Update()
    {
        base.Update();
        MoveCamera();
        if (fasterWhenTargetAtScreenEdge)
        {
            _targetOffCenterRatio = GetOffCenterRatio();
            _rollAccModifier = _targetOffCenterRatio <= 0.5f ? 1 : (1 + math.pow((_targetOffCenterRatio - 0.5f) * 10, speedBoostExponential));
        }
            
        if(playerOffCenter==0) RollCamera();
        else {
            if(isMovingToCenter) {
                RollCamera();
                isMovingToCenter = !IsTargetInScreenCenter();
            } else {
                isMovingToCenter = !IsTargetInFocusRadius();
            }
        }
    }

    private float _targetOffCenterRatio;

    [ReadOnly,SerializeField] private bool isMovingToCenter = true;
    public bool LostTarget() => !_lockManager.LockTarget;
    private bool IsTargetInFocusRadius() => IsTargetInScreenRadius(focusRadius);
    private bool IsTargetInScreenCenter() => IsTargetInScreenRadius(0.05f);

    private bool IsTargetInScreenRadius(float radius) {
        if(LostTarget()) return false;
        var offcenterRatio = fasterWhenTargetAtScreenEdge ? _targetOffCenterRatio : GetOffCenterRatio(); 
        return offcenterRatio < radius;
    }

    private void MoveCamera() {
        if(LostTarget()) return;

        if(delayTimer>0) {
            FocusPointDecelerate();
            delayTimer -= Time.deltaTime;
            return;
        }

        var targetToPlayer = _lockManager.LockTarget.position - player.position;
        var dist = targetToPlayer.magnitude;
        var camHeight = GetCameraHeight(dist);

        float centerDist = 1;
        bool isCenter = dist < centerDist;

        bool noOffCenter = playerOffCenter == 0;

        Vector3 focusRelativeToPlayer = Vector3.zero;
        if(!isCenter && !noOffCenter) {
            float offCenterDist = playerOffCenter * offCenterCurve.Evaluate(dist);
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

            if(!rightSide && distLeft - distRight > switchSideTolerance) {
                rightSide = true;
            } else if(rightSide && distRight - distLeft > switchSideTolerance) {
                rightSide = false;
            }

            camTarget = rightSide ? rightCamTarget : leftCamTarget;
        }

        if(FocusPointChase(camTarget)) {
            delayTimer = followDelay;
        }

    }
    
    private float GetOffCenterRatio()
    {
        Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(_lockManager.LockTarget.position);
        var screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);
        var screenEdgeDistance = math.min(screenCenter.x, screenCenter.y);
        return Vector2.Distance(targetScreenPos, screenCenter) /  screenEdgeDistance;
    }

    Vector3 _currentOffset;
    private float GetCameraHeight(float dist)
    {
        var camHeight = camHeightCurve.Evaluate(dist);
        UpdateCurrentOffset(dist);
        SetFollowOffset(_currentOffset);
        return camHeight;
    }

    private void UpdateCurrentOffset(float dist) {
        _currentOffset = cameraOffSet - new Vector3(0,0, camBackwardCurve.Evaluate(dist));
    }

    public override Vector3 Offset => _currentOffset;

    private Vector3 GetFocusPointLocation(Vector3 focusRelativeToPlayer, float camHeight) {
        return new Vector3(player.position.x + focusRelativeToPlayer.x, player.position.y + camHeight,
                           player.position.z + focusRelativeToPlayer.z);
    }

    private float _rollAccModifier = 1;
    private void RollCamera() {
        if(!_lockManager.LockTarget) return;
        var sqrDist = (_lockManager.LockTarget.position - player.position).sqrMagnitude;
        var dir = _lockManager.LockTarget.position - focusPoint.position;

        if(sqrDist > minViewAngleOutOfDistance.y * minViewAngleOutOfDistance.y &&
           dir.y < 0) {
            //out of range
            dir.y = 0;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir);

        var roll =Quaternion.RotateTowards(focusPoint.rotation,targetRot,rollSpeed * Time.deltaTime);
        focusPoint.eulerAngles = new Vector3(roll.eulerAngles.x, roll.eulerAngles.y, 0);

        if(Quaternion.Angle(focusPoint.rotation, targetRot) > closeUpNoRollAngle) {
            RollAccelerate(_rollAccModifier);
        } else {
            rollSpeed = rollStartSpeed;
        }
    }

    public override void OnEnter(CameraTransitionType transitionType) {
        base.OnEnter(transitionType);
        var targetToPlayer = _lockManager.LockTarget.position - player.position;
        UpdateCurrentOffset(targetToPlayer.magnitude);
        if(transitionType != CameraTransitionType.NONE) MatchPrevCamPosition();
        if(transitionType == CameraTransitionType.MATCH_LAST_ROT) {
            DebugUtil.Warn("Unsupported transition type " + CameraTransitionType.MATCH_LAST_ROT);
        }
        isMovingToCenter = true;
    }

    protected override Transform CameraFocusSpawnPoint() => player;

    public bool LockTargetExist() => _lockManager.LockTargetExist();

}
