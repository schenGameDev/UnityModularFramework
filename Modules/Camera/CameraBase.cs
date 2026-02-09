using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using Unity.Cinemachine;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CinemachineCamera), typeof(Marker))]
public abstract class CameraBase : MonoBehaviour,IMark
{

    [Header("Config")]

    [SerializeField,HideInChildren(typeof(EmptyCamera))] Transform _cameraFocusPointGroup;
    [SerializeField,HideInChildren(typeof(EmptyCamera))] protected Transform focusPoint;
    [SerializeField] protected Color gizmosColor = Color.blue;
    public bool IsDefaultCamera;

    protected Autowire<CameraManagerSO> cameraManager = new ();

    [ReadOnly] public CameraType type;

    [SerializeField] public float povChangeSpeed = 20;

    [HideInChildren(typeof(EmptyCamera))] public virtual Vector3 Offset => Vector3.zero;

#region General
    [SerializeField,Rename("POV Change Speed During Transition")] float povDelta = 20;
    [ReadOnly] public Vector3 Momentum;
    
#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif

    public virtual void OnEnter(CameraTransitionType transitionType)
    {
        enabled = true;
    }

    public virtual void OnExit() {
        SaveCurrentPosAndRot();
        ResetPOV();
        enabled = false;
    }
    
    protected virtual void Start()
    {
        _pov = vc.Lens.FieldOfView;
        POV = _pov;
    }

    protected virtual void Update() {
        UpdatePOV();
    }

    protected virtual void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        
    }
    #endregion

    #region Last State
    public Vector3 LastCamPos {get; private set;}
    public Quaternion LastCamRot {get; private set;}
    public Quaternion LastFocusRot {get; private set;}
    public float POV {get; private set;}
    private float _pov;
    [SerializeField,Self] private CinemachineCamera vc;

    public void SaveCurrentPosAndRot() {
        LastCamPos = transform.position;
        LastCamRot = transform.rotation;
        if(focusPoint) LastFocusRot = focusPoint.rotation;
        else LastFocusRot = LastCamRot;
    }
#endregion

#region Transition
    protected virtual void MatchPrevCamPosition() {
        if(!cameraManager.Get().PrevCamera) return;
        var prevCam = cameraManager.Get().PrevCamera.GetComponent<CameraBase>();
        if(prevCam.focusPoint && focusPoint) {
            var t = FindFocusPointPositionAndFwdDirectionByCamera(prevCam.LastCamPos, prevCam.LastCamRot);
            focusPoint.SetPositionAndRotation(t.Item1, t.Item2);
            RestrainMomentum(prevCam.Momentum);
        }
        POV = prevCam.POV;
        vc.Lens.FieldOfView = POV;
    }

    private Tuple<Vector3,Quaternion> FindFocusPointPositionAndFwdDirectionByCamera(Vector3 camPos,Quaternion camRot) {
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
        povDelta = povChangeSpeed * Time.deltaTime;
        if(POV < _pov) POV = Mathf.Min(POV + povDelta, _pov);
        else POV = Mathf.Max(POV - povDelta, _pov);
        vc.Lens.FieldOfView = POV;
    }
    
    protected void ResetPOV() => vc.Lens.FieldOfView = _pov;
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
        CameraShakeTask(core, positionOffsetStrength, rotationOffsetStrength, seconds, _cts.Token).Forget();
    }

    async UniTaskVoid CameraShakeTask(Transform core, Vector2 positionOffsetStrength, float rotationOffsetStrength,float duration, CancellationToken token)
    {
        float elapsed = 0f;
        float currentMagnitude = 1f;
        bool isCancelled = false;
        while (elapsed < duration && !isCancelled)
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

            await UniTask.NextFrame(cancellationToken: token);
        }

        ResetCore(core); // no need to reset if canceled
    }

    void ResetCore(Transform core) {
        core.localPosition = Vector3.zero;
        core.localRotation = Quaternion.identity;
    }
#endregion

    #region IRegistrySO
    public virtual List<Type> RegisterSelf(HashSet<Type> alreadyRegisteredTypes)
    {
        if (alreadyRegisteredTypes.Contains(typeof(CameraManagerSO))) return new ();
        SingletonRegistry<CameraManagerSO>.Instance?.Register(transform);
        return new (){typeof(CameraManagerSO)};
    }

    public virtual void UnregisterSelf()
    {
        SingletonRegistry<CameraManagerSO>.Instance?.Unregister(transform);
    }
    #endregion
#if UNITY_EDITOR
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
#endif
}

public enum CameraType {
    FOLLOW, LOCK, LOOK_AT, FIXED, EMPTY,
}

public enum CameraTransitionType {
    NONE, SMOOTH, MATCH_LAST_ROT
}
