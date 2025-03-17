using Unity.Cinemachine;
using EditorAttributes;
using Random = UnityEngine.Random;
using ModularFramework;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(CinemachineCamera))]
public abstract class CameraBase : Marker
{

    [Header("Config")]

    [SerializeField,HideInChildren(typeof(EmptyCamera))] Transform _cameraFocusPointGroup;
    [SerializeField,HideInChildren(typeof(EmptyCamera))] protected Transform focusPoint;
    [SerializeField] protected Color gizmosColor = Color.blue;
    public bool IsDefaultCamera;

    protected CameraManagerSO cameraManager;

    [ReadOnly] public CameraType Type;
    [ReadOnly,SerializeField] protected bool isLive;

    [HideInChildren(typeof(EmptyCamera))] public virtual Vector3 Offset => Vector3.zero;

#region General
    [SerializeField,Rename("POV Change Speed During Transition")] float _povDelta = 20;
    [ReadOnly] public Vector3 Momentum;

    public CameraBase() {
        registryTypes = new(Type,int)[] {(typeof(CameraManagerSO), 1)};
    }

    public virtual void OnEnter(CameraTransitionType transitionType) {
        isLive = true;
    }

    public virtual void OnExit() {
        isLive = false;
        SaveCurrentPosAndRot();
    }

    protected override void Start()
    {
        cameraManager = GetRegistry<CameraManagerSO>().Get();
        _vc = GetComponent<CinemachineCamera>();
        _pov = _vc.Lens.FieldOfView;
        POV = _pov;
    }

    protected virtual void Update() {
        UpdatePOV();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _cts?.Cancel();
    }
    #endregion

    #region Last State
    public Vector3 LastCamPos {get; private set;}
    public Quaternion LastCamRot {get; private set;}
    public Quaternion LastFocusRot {get; private set;}
    public float POV {get; private set;}
    private float _pov;
    private CinemachineCamera _vc;

    public void SaveCurrentPosAndRot() {
        LastCamPos = transform.position;
        LastCamRot = transform.rotation;
        if(focusPoint!=null) LastFocusRot = focusPoint.rotation;
        else LastFocusRot = LastCamRot;
    }
#endregion

#region Transition
    protected virtual void MatchPrevCamPosition() {
        if(cameraManager.PrevCamera==null) return;
        var prevCam = cameraManager.PrevCamera.GetComponent<CameraBase>();
        if(prevCam.focusPoint!=null && focusPoint!=null) {
            var t = FindFocusPointPositionAndFwdDirectionByCamera(prevCam.LastCamPos, prevCam.LastCamRot);
            focusPoint.SetPositionAndRotation(t.Item1, t.Item2);
            RestrainMomentum(prevCam.Momentum);
        }
        POV = prevCam.POV;
        _vc.Lens.FieldOfView = POV;
    }

    protected Tuple<Vector3,Quaternion> FindFocusPointPositionAndFwdDirectionByCamera(Vector3 camPos,Quaternion camRot) {
        Quaternion focusRot = camRot * Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward,-Offset));
        Vector3 focusPos = camPos - focusRot * Offset;
        // var camFwd = camRot * Vector3.forward;
        // Vector3 focusPos = Offset.magnitude * camFwd + camPos;
        return new(focusPos, focusRot);
    }
    protected virtual void RestrainMomentum(Vector3 inheritedMomentum) {
        Momentum = inheritedMomentum;
    }
    protected void UpdatePOV() {
        if(POV == _pov) return;
        _povDelta = 20 * Time.deltaTime;
        if(POV < _pov) POV = Mathf.Min(POV + _povDelta, _pov);
        else POV = Mathf.Max(POV - _povDelta, _pov);
        _vc.Lens.FieldOfView = POV;
    }
#endregion

#region Shake
    private CancellationTokenSource _cts;
    public void Shake(Vector2 positionOffsetStrength, float rotationOffsetStrength, float seconds) {
        Transform core = focusPoint == null? transform : focusPoint.GetChild(0);
        if(_cts!=null) {
            _cts.Cancel();
            _cts.Dispose();
            ResetCore(core);
        }
        _cts = new CancellationTokenSource();
        CameraShakeCoroutine(core, positionOffsetStrength, rotationOffsetStrength, seconds, _cts.Token).Forget();
    }

    async UniTaskVoid CameraShakeCoroutine(Transform core, Vector2 positionOffsetStrength, float rotationOffsetStrength,float duration, CancellationToken token)
    {
        float elapsed = 0f;
        float currentMagnitude = 1f;

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();
            float x = (Random.value - 0.5f) * currentMagnitude * positionOffsetStrength.x;
            float y = (Random.value - 0.5f) * currentMagnitude * positionOffsetStrength.y;

            float lerpAmount = currentMagnitude * rotationOffsetStrength;
            Vector3 lookAtVector = Vector3.Lerp(Vector3.forward, Random.insideUnitCircle, lerpAmount);

            core.localPosition = new Vector3(x, y, 0);
            core.localRotation = Quaternion.LookRotation(lookAtVector);

            elapsed += Time.deltaTime;
            currentMagnitude = (1 - (elapsed / duration)) * (1 - (elapsed / duration));

            await UniTask.NextFrame(cancellationToken: token).SuppressCancellationThrow();
        }

        ResetCore(core);
    }

    void ResetCore(Transform core) {
        core.localPosition = Vector3.zero;
        core.localRotation = Quaternion.identity;
    }
#endregion

    #region Editor
    protected abstract Transform CameraFocusSpawnPoint();

    [Button]
    protected virtual void CreateFocusPoint()
    {
        // Destroy existing
        if(focusPoint!=null) {
            DestroyImmediate(focusPoint.gameObject);
        }


        focusPoint = new GameObject(gameObject.name + "FocusPoint").transform;
        focusPoint.position = CameraFocusSpawnPoint()==null? Vector3.zero : CameraFocusSpawnPoint().position;
        focusPoint.rotation = CameraFocusSpawnPoint()==null? Quaternion.identity : CameraFocusSpawnPoint().rotation;
        if(_cameraFocusPointGroup!=null) focusPoint.parent = _cameraFocusPointGroup;

        var core = new GameObject(gameObject.name +"Core").transform;
        core.parent = focusPoint;
        core.localPosition = Vector3.zero;
        core.localRotation = Quaternion.identity;

        CinemachineSetUp();
    }

    protected virtual void CinemachineSetUp() {
        var cm = GetComponent<CinemachineCamera>();
        cm.Target.TrackingTarget = focusPoint.GetChild(0);
    }


    protected virtual void OnDrawGizmos() {
        if(focusPoint==null) return;
        Gizmos.color = gizmosColor;
        var pos = focusPoint.position;
        var forward = focusPoint.forward * 3;
        Gizmos.DrawRay(pos, forward);

        Vector3 right = Quaternion.LookRotation(forward) * Quaternion.Euler(0,180+20,0) * new Vector3(0,0,1);
        Vector3 left = Quaternion.LookRotation(forward) * Quaternion.Euler(0,180-20,0) * new Vector3(0,0,1);
        Gizmos.DrawRay(pos + forward, right * 0.25f);
        Gizmos.DrawRay(pos + forward, left * 0.25f);
    }
    #endregion
}

public enum CameraType {
    FOLLOW, LOCK, LOOK_AT, FIXED, EMPTY,
}

public enum CameraTransitionType {
    NONE, SMOOTH, MATCH_LAST_ROT
}
