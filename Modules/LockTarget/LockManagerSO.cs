using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using UnityEngine;
using ModularFramework;

[CreateAssetMenu(fileName ="LockManager_SO",menuName ="Game Module/Target Lock")]
public class LockManagerSO : GameModule, IRegistrySO
{
    [Header("Config")]
    [SerializeField] private float _maxLockDistance = 10;
    [SerializeField] private bool _autoLock = true;

    [FoldoutGroup("Event Channels", nameof(_cameraManager), nameof(_lockEvent), nameof(_switchLockTargetEvent))]
    [SerializeField] private EditorAttributes.Void _eventChannelGroup;

    [HideInInspector,SerializeField] private CameraManagerSO _cameraManager;
    [HideInInspector,SerializeField] private EventChannel _lockEvent;
    [HideInInspector,SerializeField] private EventChannel<bool> _switchLockTargetEvent;

    [Header("Runtime")]
#if UNITY_EDITOR
    [ReadOnly,SerializeField,RuntimeObject] private string[] _lockableInScene;
#endif
    [ReadOnly,RuntimeObject] public bool IsLock;
    [ReadOnly,RuntimeObject] public Transform LockTarget;

    [RuntimeObject] private HashSet<Transform> _lockableSet=new();
    [RuntimeObject] private Transform Player;
    [RuntimeObject] private string _lockCameraName;

    public LockManagerSO() {
        RefKeywords = new[]{"PLAYER","LOCK_CAMERA"};
        updateMode = UpdateMode.EVERY_N_FRAME;
    }

    public override void OnAwake(Dictionary<string, string> flags, Dictionary<string, GameObject> references)
    {
        base.OnAwake(flags, references);
        Player = references["PLAYER"].transform;
        _lockCameraName = references["LOCK_CAMERA"].name;
    }
    private void OnEnable() {
        _lockEvent.AddListener(LockUnlock);
        _switchLockTargetEvent.AddListener(SwitchTarget);
    }

    private void OnDisable() {
        _lockEvent.RemoveListener(LockUnlock);
        _switchLockTargetEvent.RemoveListener(SwitchTarget);
    }

    public void LockUnlock()
    {
        if(IsLock) {
            // unlock
            if(LockTarget!=null) LockTarget.GetComponent<Lockable>().Lock(false);
            LockTarget = null;
            _cameraManager.BackToPrevCamera(CameraTransitionType.SMOOTH);
        } else {
            // lock
            if(_lockableSet==null || _lockableSet.Count==0) return;

            List<(Transform,float)> lockableInCamera = GetLockableInCamera();

            if(lockableInCamera.Count==0) return;

            LockTarget = lockableInCamera[0].Item1;
            LockTarget.GetComponent<Lockable>().Lock(true);

            _cameraManager.CameraTransitionTo(_lockCameraName,CameraTransitionType.SMOOTH);
        }

        IsLock = !IsLock;
    }

    private List<(Transform, float)> GetLockableInCamera() {
        return _lockableSet
                .Where(l => Vector3.Distance(Player.position,l.position) < _maxLockDistance)
                .Select(l => (l, l.GetComponent<Lockable>().DisToScreenCenter()))
                .Where(t => t.Item2 < 1)
                .OrderBy(t => t.Item2)
                .ToList();
    }

    [RuntimeObject] private bool _isAutoLockedBefore;
    public void CheckLockInDistance() {
        if(IsLock && (LockTarget == null ||
                      Vector3.Distance(Player.position, LockTarget.position) > _maxLockDistance)) {
            LockUnlock();
        } else if(!IsLock && _autoLock && !_isAutoLockedBefore) {
            LockUnlock();
            if(IsLock) _isAutoLockedBefore = true;
        } else if(!IsLock && _isAutoLockedBefore && GetLockableInCamera().Count == 0) {
            _isAutoLockedBefore = false; // reset
        }
    }

    public bool LockTargetExist() => GetLockableInCamera().Count > 0;

    public void Register(Transform lockable) {
        if(_lockableSet.Contains(lockable)) return;
        _lockableSet.Add(lockable);
        DisplayLockableNames();
    }

    public void Unregister(Transform lockable) {
        if(!_lockableSet.Contains(lockable)) return;
        _lockableSet.Remove(lockable);
        DisplayLockableNames();
    }

    private void DisplayLockableNames() {
#if UNITY_EDITOR
        _lockableInScene = _lockableSet.Select(x => x.name).ToArray();
#endif
    }

    public void TargetLost() {
        if(IsLock) LockUnlock();
    }

    public void SwitchTarget(bool isRightSide) { // + right, - left
        if(!IsLock || _lockableSet==null || _lockableSet.Count<=1) return;
        List<Transform> lockableInCamera = _lockableSet
                .Select(l=> new Tuple<Transform,float>(l, l.GetComponent<Lockable>().HorizontalDisToScreenLeft()))
                .Where(l=>l.Item2<=1)
                .OrderBy(l=>l.Item2)
                .Select(l=>l.Item1)
                .ToList();
        if(LockTarget==null || lockableInCamera.Count<=1) return;

        Transform newTarget = null;
        Transform prev=lockableInCamera.Last();
        foreach(var l in lockableInCamera) {
            if(!isRightSide && l == LockTarget) {
                newTarget = prev;
                break;
            } else if(isRightSide && prev==LockTarget) {
                newTarget = l;
                break;
            }
            prev = l;
        }
        if(newTarget == null && prev ==LockTarget) newTarget = lockableInCamera[0];

        if(newTarget!=null) {
            LockTarget.GetComponent<Lockable>().Lock(false);
            LockTarget = newTarget;
            LockTarget.GetComponent<Lockable>().Lock(true);
        }


    }

    private float CalculateDistanceToCamera(Transform lockable) {
        return Vector3.Distance(lockable.position,Camera.main.transform.position);
    }
}
