using EditorAttributes;
using UnityEngine;

/// <summary>
/// raise camera and look at target
/// </summary>
public class LookAtCamera : MovingCameraBase
{
    [SerializeField] Transform target;
    [SerializeField] Transform player;
    [SerializeField] bool zoomIn;

    [SerializeField,Rename("Camera Height"),Suffix("m")] private float camHeight=0;
    [SerializeField,Rename("Camera Distance To Target"),Suffix("m"), ShowField(nameof(zoomIn))] private float camDist;

    private Vector3 _camTarget;
    
    public LookAtCamera() {
        type = CameraType.LOOK_AT;
    }

    protected override Transform CameraFocusSpawnPoint() => player;

    public override void OnEnter(CameraTransitionType transitionType)
    {
        base.OnEnter(transitionType);
        if(zoomIn) {
            var targetToPlayerVec = player.position -target.position;
            targetToPlayerVec.y = 0;
            _camTarget = target.position + new Vector3(0,camHeight,0) + camDist * targetToPlayerVec.normalized;
        }
    }
    protected override void Update()
    {
        base.Update();
        MoveCamera();
        RollCamera();
    }
    
    private void MoveCamera() {
        if(delayTimer>0) {
            FocusPointDecelerate();
            delayTimer -= Time.deltaTime;
            return;
        }

        Vector3 camTarget = zoomIn? _camTarget : player.position + new Vector3(0,camHeight,0);


        if(FocusPointChase(camTarget)) {
            delayTimer = followDelay;
        }

    }

    void RollCamera() {
        var dir = target.position - focusPoint.position;
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