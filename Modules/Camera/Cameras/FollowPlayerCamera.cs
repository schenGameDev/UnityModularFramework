using System;
using EditorAttributes;
using Unity.Mathematics;
using UnityEngine;
using ModularFramework;
using Void = EditorAttributes.Void;

public class FollowPlayerCamera : MovingCameraBase
{
    [SerializeField] Transform _player;
    [MinMaxSlider(-90.0f, 90.0f),SerializeField] private Vector2 _minMaxCameraAngle = new Vector2(-10, 30);
    [FoldoutGroup("Controller",nameof(_deadZone))]
    [SerializeField] private Void controllerGroup;
    [HideInInspector,SerializeField] private Vector2 _deadZone = new(0.01f,0.3f);


    [FoldoutGroup("Camera Height",nameof(_camHeight))]
    [SerializeField] private Void cameraHeightGroup;
    [FoldoutGroup("Default View",nameof(_defaultTarget),nameof(_defaultAngle))]
    [SerializeField] private Void defaultGroup;


    [HideInInspector,SerializeField,Rename("Camera Height"),Suffix("m")] private float _camHeight=0;
    [HideInInspector,SerializeField,Rename("Target")] private GameObject _defaultTarget;
    [HideInInspector,SerializeField,Rename("Angles"), HelpBox("Angles are inactive if Target is set", MessageMode.None)] private Vector3 _defaultAngle;

    private Autowire<InputSystemSO> _inputSystem = new();

    private Vector2 _lookDeltaMovement => _inputSystem.Get().LookDeltaMovement;

    public Vector3 FreeMoveViewDirection => focusPoint.forward;
    
    public override Type[][] RegistryTypes =>new[] {new[] {typeof(CameraManagerSO)}, new[] {typeof(InputSystemSO)}};

    public FollowPlayerCamera() {
        type = CameraType.FOLLOW;
        cameraOffSet = new(0,1,-5);
    }
    protected override void Start() {
        base.Start();
        
        focusPoint.position = _player.position + new Vector3(0,_camHeight,0);
        if(_defaultTarget != null) {
            focusPoint.LookAt(_defaultTarget.transform.position);
        } else {
            focusPoint.eulerAngles = _defaultAngle;
        }
    }

    protected override void Update()
    {
        base.Update();
        MoveCamera();
        RollCamera();
    }

    private void MoveCamera() {
        var targetPos = _player.position + new Vector3(0,_camHeight,0);
        if(Vector3.SqrMagnitude(focusPoint.position - targetPos) <= 0.001f) {
            FocusPointDecelerate();
            delayTimer = followDelay;
            return;
        }
        if(delayTimer>0) {
            FocusPointDecelerate();
            delayTimer -= Time.deltaTime;
            return;
        }
        if(FocusPointChase(targetPos)) {
            delayTimer = followDelay;
        }
    }

    private void RollCamera() {
        // change speed
        bool isStop = _lookDeltaMovement.sqrMagnitude < 0.1f;
        if(isStop) {
            if(_matchLastFocusRot) {
                focusPoint.rotation = Quaternion.RotateTowards(focusPoint.rotation,  LastFocusRot, rollSpeed * Time.deltaTime);
                if(focusPoint.rotation != LastCamRot) {
                    RollAccelerate();
                } else {
                    RollDecelerate();
                    _matchLastFocusRot = false;
                }
            } else {
                RollDecelerate();
            }

            return;
        }

        Vector2 rollDir = new(math.abs(_lookDeltaMovement.x) > _deadZone.x? _lookDeltaMovement.x : 0,
                              math.abs(_lookDeltaMovement.y) > _deadZone.y? _lookDeltaMovement.y : 0);
        isStop = rollDir.sqrMagnitude < 0.1f;
        if(isStop) {
            RollDecelerate();
        } else {
            RollAccelerate();
        }

        // horizontal | y rotation axis
        float deltaYAxis = rollDir.x * rollSpeed * Time.deltaTime / rollDir.magnitude;
        // vertical | x rotation axis
        float deltaXAxis = - rollDir.y * rollSpeed * Time.deltaTime / rollDir.magnitude;

        float x = (focusPoint.eulerAngles.x > 180? focusPoint.eulerAngles.x - 360 : focusPoint.eulerAngles.x) + deltaXAxis;
        x= Mathf.Clamp(x,_minMaxCameraAngle.x,_minMaxCameraAngle.y);

        float y = focusPoint.eulerAngles.y + deltaYAxis;

        focusPoint.eulerAngles = new(x, y, 0);

    }

    bool _matchLastFocusRot;
    public override void OnEnter(CameraTransitionType transitionType) {
        base.OnEnter(transitionType);
        if(transitionType != CameraTransitionType.NONE) MatchPrevCamPosition();
        if(transitionType == CameraTransitionType.MATCH_LAST_ROT) _matchLastFocusRot = true;

    }

    public Vector3 FwdVectorRelativeToCamera(Vector3 faceDirection) {
        var cameraFwd = new Vector3(FreeMoveViewDirection.x,0,FreeMoveViewDirection.z);
        var degWithRef = Vector3.SignedAngle(cameraFwd, Vector3.forward, Vector3.up);
        var relativeVec = Quaternion.AngleAxis(degWithRef,Vector3.up) * faceDirection;
        return relativeVec.normalized;

    }

    protected override Transform CameraFocusSpawnPoint() => _player;


    public void TempChangeMaxAngle(Vector2 minMaxAngle) {
        _minMaxCameraAngle = minMaxAngle;
    }

}
