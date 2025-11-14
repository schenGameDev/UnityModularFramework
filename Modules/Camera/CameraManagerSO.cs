
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using EditorAttributes;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using ModularFramework;
using ModularFramework.Commons;
using ModularFramework.Utility;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName ="CameraManager_SO",menuName ="Game Module/Camera")]
public class CameraManagerSO : GameModule,IRegistrySO {
    [Header("Config")]
    [SerializedDictionary("Transition","Acceleration")] public SerializedDictionary<Vector<string>, float> transitionAcceleration;
    [SerializeField] Vector2 positionShakeStrength = new (0.03f,0.03f);
    [SerializeField] float  rotationShakeStrength = 0.05f;
    [SerializeField] float shakeTime = 0.3f;



    [Header("Runtime")]
    [SerializeField,ReadOnly,RuntimeObject] private string currentCamera;
    [SerializeField,ReadOnly,RuntimeObject] private string prevCamera;
    [ReadOnly] public CameraType currentMode;


    [RuntimeObject] public Transform CurrentCamera {get; private set;}

    [RuntimeObject] public Transform PrevCamera {get; private set;}

    [RuntimeObject] private Dictionary<string, Tuple<CameraType,Transform>> _activeCamerasInScene=new();

    [RuntimeObject] private string _defaultCamera;

    public CameraManagerSO() {
        updateMode = UpdateMode.NONE;
    }

    protected override void Reset() {
        base.Reset();
        currentMode = CameraType.FIXED;
    }

    [RuntimeObject] OnetimeFlip _isDefaultSet = new();
    public void Register(Transform transform) {
        string name = transform.name;
        CameraBase cb = transform.GetComponent<CameraBase>();

        if(cb == null) return;
        CameraType type = cb.type;
        if(_activeCamerasInScene.ContainsKey(name)) {
            DebugUtil.Error("Camera fail to register because name duplicates");
            return;
        }
        _activeCamerasInScene.Add(name, new(type, transform));
        if(cb.IsDefaultCamera) {
            if(_isDefaultSet) {
                DebugUtil.Error("Check all virtual cameras, cannot have 2 default cameras in scene.");
                SetInactiveCamera(transform);
            } else {
                SetCurrentCamera(transform, CameraTransitionType.NONE);
                _defaultCamera = name;
            }
        } else {
            SetInactiveCamera(transform);
        }

    }

    public void Unregister(Transform transform) {
        string name = transform.name;
        if(!_activeCamerasInScene.ContainsKey(name)) return;
        var temp = _activeCamerasInScene[name];
        if( temp != null) {
            var cam = temp.Item2;
            if(CurrentCamera == cam) {
                CurrentCamera = null;
                currentCamera = "";
                currentMode = CameraType.FIXED;
            }
            if(PrevCamera == cam) {
                PrevCamera = null;
                prevCamera = "";
            }
        }
        _activeCamerasInScene.Remove(name);
    }
#region Camera Transition
    public void CameraTransitionToDefault(CameraTransitionType transitionType) => CameraTransitionTo(_defaultCamera, transitionType);

    [Button]
    public void CameraTransitionTo(string targetCamera, CameraTransitionType transitionType) {
        if(!_activeCamerasInScene.ContainsKey(targetCamera)) return;
        var target = _activeCamerasInScene[targetCamera];
        if(target == null) return;
        if(!IsCameraReady(PrevCamera)) {
            CameraTransitionToDefault(transitionType);
            return;
        }
        if(CurrentCamera!=null) {
            PrevCamera = CurrentCamera;
            SetInactiveCamera(PrevCamera);
            prevCamera = currentCamera;
            PrevCamera.GetComponent<CameraBase>().OnExit();
        }
        SetCurrentCamera(target.Item2, transitionType);
    }
    [Button]
    public void BackToPrevCamera(CameraTransitionType transitionType) {
        if(CurrentCamera==null || PrevCamera==null) return;
        if(!IsCameraReady(PrevCamera)) {
            CameraTransitionToDefault(transitionType);
            return;
        }
        var temp = PrevCamera;
        PrevCamera = CurrentCamera;
        SetInactiveCamera(PrevCamera);
        PrevCamera.GetComponent<CameraBase>().OnExit();
        prevCamera = PrevCamera.name;

        SetCurrentCamera(temp, transitionType);
    }
#endregion

    public bool IsCameraReady(Transform camera) {
        var cam = camera.GetComponent<LockTargetCamera>();
        return cam==null || cam.LockTargetExist();
    }

    private void SetCurrentCamera(Transform camera, CameraTransitionType transitionType) {
        CurrentCamera = camera;
        var camBase = CurrentCamera.GetComponent<CameraBase>();
        currentMode = camBase.type;
        currentCamera = CurrentCamera.name;
        camBase.OnEnter(transitionType);
        CurrentCamera.GetComponent<CinemachineCamera>().Priority = 11;
    }

    private void SetInactiveCamera(Transform camera) {
        camera.GetComponent<CinemachineCamera>().Priority = 10;
        
    }

    [Button]
    public void Shake() => Shake(positionShakeStrength,rotationShakeStrength,shakeTime);

    public void Shake(Vector2 positionOffsetStrength, float rotationOffsetStrength, float seconds) {
        foreach(var cam in _activeCamerasInScene.Values.Select(t => t.Item2.GetComponent<CameraBase>())) {
            cam.Shake(positionOffsetStrength, rotationOffsetStrength, seconds);
        }
    }
}

