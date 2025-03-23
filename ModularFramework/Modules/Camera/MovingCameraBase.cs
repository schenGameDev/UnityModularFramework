using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using ModularFramework.Commons;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineRotateWithFollowTarget),typeof(CinemachineFollow))]
public abstract class MovingCameraBase : CameraBase {
    [SerializeField,OnValueChanged(nameof(UpdateFollowOffset))] protected Vector3 cameraOffSet = new(0,0,0);
    [FoldoutGroup("Camera Follow",nameof(followAcceleration), nameof(friction), nameof( followMaxSpeed), nameof(followDelay))]
    [SerializeField] private Void cameraFollowGroup;

    [HideInInspector,SerializeField,Rename("Friction"),Suffix("m/s2")] protected float friction=0.3f;
    [HideInInspector,SerializeField,Rename("Follow Max Speed"),Suffix("m/s")] protected float followMaxSpeed = 10;
    [HideInInspector,SerializeField,Rename("Acceleration"),Suffix("m/s2")] protected float followAcceleration=2;
    [HideInInspector,SerializeField,Rename("Delay"),Suffix("s")] protected float followDelay=0;

    [FoldoutGroup("Camera Roll",nameof(rollStartSpeed), nameof(rollMaxSpeed),nameof(rollAcceleration))]
    [SerializeField] private Void cameraRollGroup;
    [HideInInspector,Rename("Start Speed"),SerializeField,Suffix("deg/s")] protected float rollStartSpeed=10;
    [HideInInspector,Rename("Max Speed"),SerializeField,Suffix("deg/s")] protected float rollMaxSpeed=60;
    [HideInInspector,Rename("Acceleration"),SerializeField,Suffix("deg/s2")] protected float rollAcceleration=20;

    public override Vector3 Offset => cameraOffSet;

    protected float delayTimer,rollSpeed;

    protected virtual void Awake() {
        delayTimer = followDelay;
        rollSpeed = rollStartSpeed;

        _ogFollowAcc = followAcceleration;
        _ogFollowMax = followMaxSpeed;
        _ogRollAcc = rollAcceleration;
        _ogRollMax = rollMaxSpeed;
    }

    public override void OnExit() {
        base.OnExit();
        rollSpeed = rollStartSpeed;
        delayTimer = followDelay;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        _cts?.Cancel();
        _cts?.Dispose();
    }

    protected override void CinemachineSetUp()
    {
        base.CinemachineSetUp();

        UpdateFollowOffset();
    }

    protected void UpdateFollowOffset()  => SetFollowOffset(cameraOffSet);

    protected void SetFollowOffset(Vector3 offset)
    {
        var follow = GetComponent<CinemachineFollow>();
        if (follow != null) {
            follow.FollowOffset = offset;
            follow.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.LockToTarget;
            follow.TrackerSettings.PositionDamping = Vector3.zero;
            follow.TrackerSettings.RotationDamping = Vector3.zero;
        }

        var followRot = GetComponent<CinemachineRotateWithFollowTarget>();
        if (followRot != null) {
            followRot.Damping = 0;
        }
    }

    protected override void MatchPrevCamPosition()
    {
        base.MatchPrevCamPosition();
        var prevName = cameraManager.PrevCamera.name;
        var curName = this.gameObject.name;
        if(cameraManager.TransitionAcceleration.TryGetValue(new Vector<string>(prevName,curName), out float accModifier)) {
            TempChangeAcceleration(accModifier);
        }
    }

    CancellationTokenSource _cts;
    float _ogFollowAcc, _ogFollowMax,_ogRollAcc,_ogRollMax;
    protected void TempChangeAcceleration(float accelerationModifier)
    {
        if(_cts!=null) {
            _cts.Cancel();
            _cts.Dispose();
        }
        _cts = new CancellationTokenSource();
        ChangeAcceleration(accelerationModifier, _cts.Token).Forget();
    }

    async UniTaskVoid ChangeAcceleration(float tempAccelerationModifier, CancellationToken token) {
        
        followAcceleration =  _ogFollowAcc * tempAccelerationModifier;
        rollAcceleration = _ogRollAcc * tempAccelerationModifier;
        followMaxSpeed = 1000;
        rollMaxSpeed = 1000;
        rollSpeed = 0;
        await UniTask.Delay(1, cancellationToken: token).SuppressCancellationThrow();
        ResetAcceleration();
    }

    void ResetAcceleration() {
        followAcceleration = _ogFollowAcc;
        followMaxSpeed = _ogFollowMax;
        rollAcceleration = _ogRollAcc;
        rollMaxSpeed = _ogRollMax;
    }

    protected bool FocusPointChase(Vector3 target, float acceleration, float friction, float maxSpeed) {
        Vector3 diff = target - focusPoint.position;
        float sqrDiff = diff.sqrMagnitude;
        if(sqrDiff < 0.001f) {
            focusPoint.position = target;
            FocusPointDecelerate(acceleration);
            return true;
        }

        FocusPointDecelerate(friction);

        Vector3 force =  acceleration * Time.deltaTime * diff.normalized;
        Momentum += force;
        float sqrDist = Momentum.sqrMagnitude;
        Vector3 maxDelta = Time.deltaTime * maxSpeed * Momentum.normalized;
        float sqrMax = maxDelta.sqrMagnitude;

        if(sqrDist > sqrMax) {
            Momentum = maxDelta;
            sqrDist = sqrMax;
        }

        bool isArrive= sqrDist >= sqrDiff;
        if(isArrive) {
            focusPoint.position = target;
            return true;
        }
        focusPoint.position += Momentum;
        return false;
    }

    protected void FocusPointDecelerate(float deceleration) {
        if(Momentum == Vector3.zero) return;

        Vector3 dir = Momentum.normalized;
        Vector3 force = deceleration * Time.deltaTime * dir;
        bool isArrive= Momentum.sqrMagnitude <= force.sqrMagnitude;
        if(isArrive) {
            Momentum = Vector3.zero;
            return;
        }
        Momentum -= force;
    }

    protected bool FocusPointChase(Vector3 target) => FocusPointChase(target, followAcceleration, friction, followMaxSpeed);
    protected void FocusPointDecelerate() => FocusPointDecelerate(followAcceleration + friction);

    protected void RollAccelerate() {
        if(rollSpeed<rollStartSpeed) rollSpeed = rollStartSpeed;
        else if(rollSpeed < rollMaxSpeed) rollSpeed = Mathf.Min(rollSpeed + rollAcceleration * Time.deltaTime, rollMaxSpeed);
        else if(rollSpeed > rollMaxSpeed) rollSpeed = rollMaxSpeed;
    }

    protected void RollDecelerate() {
        if(rollSpeed > rollStartSpeed) {
            rollSpeed = Mathf.Max(rollSpeed - rollAcceleration * Time.deltaTime, rollStartSpeed);
        } else if(rollSpeed < rollStartSpeed) {
            rollSpeed = rollStartSpeed;
        }
    }

    protected override void RestrainMomentum(Vector3 inheritedMomentum) {
        var maxSpeedDelta = followMaxSpeed * Time.deltaTime;
        if(inheritedMomentum.sqrMagnitude >  maxSpeedDelta * maxSpeedDelta) Momentum = maxSpeedDelta * inheritedMomentum.normalized;
        else Momentum = inheritedMomentum;
    }
}