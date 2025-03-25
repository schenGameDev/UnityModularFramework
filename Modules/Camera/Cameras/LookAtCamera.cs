using EditorAttributes;
using UnityEngine;
/// <summary>
/// raise camera and look at target
/// </summary>
public class LookAtCamera : MovingCameraBase
{
    [SerializeField] Transform _target;
    [SerializeField] Transform _player;
    [SerializeField] bool _zoomIn;

    [SerializeField,Rename("Camera Height"),Suffix("m")] private float _camHeight=0;
    [SerializeField,Rename("Camera Distance To Target"),Suffix("m"), ShowField(nameof(_zoomIn))] private float _camDist=0;


    public LookAtCamera() {
        Type = CameraType.LOOK_AT;
    }

    protected override Transform CameraFocusSpawnPoint() => _player;


    public override void OnEnter(CameraTransitionType transitionType)
    {
        base.OnEnter(transitionType);
        _isEnter = true;
    }

    protected override void Update()
    {
        base.Update();
        if(!isLive) {
            return;
        }
        if(_isEnter) {
            _isEnter = false;
            if(_zoomIn) {
                var targetToPlayerVec = _player.position -_target.position;
                targetToPlayerVec.y = 0;
                _camTarget = _target.position + new Vector3(0,_camHeight,0) + _camDist * targetToPlayerVec.normalized;
            }
        }
        MoveCamera();
        RollCamera();
    }

    bool _isEnter;
    Vector3 _camTarget;
    private void MoveCamera() {
        if(delayTimer>0) {
            FocusPointDecelerate();
            delayTimer -= Time.deltaTime;
            return;
        }

        Vector3 camTarget = _zoomIn? _camTarget : _player.position + new Vector3(0,_camHeight,0);


        if(FocusPointChase(camTarget)) {
            delayTimer = followDelay;
        }

    }

    void RollCamera() {
        var dir = _target.position - focusPoint.position;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        var roll =Quaternion.RotateTowards(focusPoint.rotation,targetRot,rollSpeed * Time.deltaTime);
        if(focusPoint.rotation != roll) {
            RollAccelerate();
        } else {
            rollSpeed = rollStartSpeed;
        }
        focusPoint.rotation = roll;
    }
}