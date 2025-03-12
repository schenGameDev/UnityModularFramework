using System;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using Unity.Mathematics;
using UnityEngine;
using ModularFramework;
using static ModularFramework.Utility.MathUtil;

/// <summary>
/// Class <c>SensorManager</c> monitors all <c>Sensible</c> in scene, calculate distance and handle vision between them
/// </summary>
[CreateAssetMenu(fileName = "SensorManager_SO", menuName = "Game Module/Sensor")]
public class SensorManagerSO : GameModule, IRegistrySO {
    //[SerializeField,SerializedDictionary("Code","parameter")] private SerializedDictionary<int,string[]> _parameters;

    [Header("Runtime")]
    [ReadOnly,SerializeField] private string[] _sensibleInScene;

    public SensorManagerSO() {
        updateMode = UpdateMode.NONE;
    }

    private Dictionary<string,Sensible> _sensibleDict;

    protected override void Reset() {
        _sensibleInScene= null;
        _sensibleDict = new();
    }


    public void Register(Transform sensible) {
        if(_sensibleDict.ContainsKey(sensible.name)) {
            Debug.LogError("Fail to register Sensible gameObject because another Senisble already uses name: " + sensible.name);
            return;
        }
        _sensibleDict.Add(sensible.name, sensible.GetComponent<Sensible>());
        DisplaySensibleNames();
    }

    public void Unregister(Transform sensible) {
        if(!_sensibleDict.ContainsKey(sensible.name)) return;
        _sensibleDict.Remove(sensible.name);
        DisplaySensibleNames();
    }

    private void DisplaySensibleNames() {
        _sensibleInScene = _sensibleDict.Keys.ToArray();
    }

    public Transform GetTransform(string name, bool overrideTargetVisible) {
        var found = _sensibleDict[name];
        if(found == null || (!overrideTargetVisible && !found.IsVisible)) return null;
        return found.transform;
    }
    public Transform GetTransformAbsoluteInRange(string targetName, Transform self, Vector2 minMaxRange) {
        return GetTransformAbsoluteInRange(targetName, self, minMaxRange, 180, true);
    }
    public Transform GetTransformAbsoluteInRange(string targetName, Transform self, Vector2 minMaxRange,
                                                 float halfConeAngle, bool overrideTargetVisible) {
        var target = GetTransform(targetName, overrideTargetVisible);
        if(target != null && WithinViewCone(target, self, halfConeAngle) && WithinViewRange(target, self, minMaxRange)) {
            return target;
        }
        return null;
    }

    public Transform GetTransformRaycastInRange(string targetName, Transform self, Vector2 minMaxRange,
                                                 float halfConeAngle, bool overrideTargetVisible) {
        var target = GetTransform(targetName, overrideTargetVisible);
        if(target!=null && WithinViewCone(target, self, halfConeAngle) && IsRaycastHit(target, self, minMaxRange, ~0)) {
            return target;
        }
        return null;
    }

    /// <summary>
    /// return a list of (target transform, <b>sqr</b>Distance) in distance asc order
    /// </summary>
    public List<Tuple<Transform, float>> GetTagAbsoluteInRange(Transform self, string tag, Vector2 minMaxRange,
                                               float halfConeAngle, bool overrideTargetVisible) {
        return _sensibleDict.Values
                    .Where(x=>(overrideTargetVisible || x.IsVisible) && x.CompareTag(tag))
                    .Where(x => WithinViewCone(x.transform, self, halfConeAngle))
                    .Select(x => new Tuple<Transform, float> (x.transform, Vector3.SqrMagnitude(self.position - x.transform.position)))
                    .Where(tup => WithinRange(tup.Item2, math.pow(minMaxRange.x,2), math.pow(minMaxRange.y,2)))
                    .OrderBy(tup => tup.Item2)
                    .ToList();
    }

    /// <summary>
    /// return a list of (target transform, distance) in distance asc order
    /// </summary>
    public List<Tuple<Transform, float>> GetTagRaycastInRange(Transform self, string tag, Vector2 minMaxRange,
                                               float halfConeAngle, bool overrideTargetVisible) {
        return _sensibleDict.Values
                    .Where(x=>(overrideTargetVisible || x.IsVisible) && x.CompareTag(tag))
                    .Where(x => WithinViewCone(x.transform, self, halfConeAngle))
                    .Select(x => {
                        if(IsRaycastHit(x.transform, self, minMaxRange, ~0, out float distance)) {
                            return new Tuple<Transform, float> (x.transform, distance);
                        }
                        return null;
                    })
                    .Where(tuple => tuple != null)
                    .OrderBy(tup => tup.Item2)
                    .ToList();
    }

    public Transform GetClosestAbsoluteInRange(Transform self, string tag, Vector2 minMaxRange,
                                               float halfConeAngle, bool overrideTargetVisible) {
        var found = GetTagAbsoluteInRange(self, tag, minMaxRange, halfConeAngle, overrideTargetVisible);
        if(found.Count == 0) return null;
        return found[0].Item1;
    }

    public Transform GetClosestRaycastInRange(Transform self, string tag, Vector2 minMaxRange,
                                               float halfConeAngle, bool overrideTargetVisible) {
        var found = GetTagRaycastInRange(self, tag, minMaxRange, halfConeAngle, overrideTargetVisible);
        if(found.Count == 0) return null;
        return found[0].Item1;
    }

    private bool WithinViewCone(Transform target, Transform self, float halfConeAngle) {
        if(halfConeAngle >= 180) return true; // 360
        Vector3 dir = target.position - self.position;
        return Vector3.Angle(self.forward, dir) < halfConeAngle;
    }

    private bool WithinViewRange(Transform target, Transform self, Vector2 minMaxRange) {
        return WithinRange(Vector3.SqrMagnitude(target.position - self.position), math.pow(minMaxRange.x,2), math.pow(minMaxRange.y,2));
    }

    private bool IsRaycastHit(Transform target, Transform self, Vector2 minMaxRange, LayerMask layerMask) {
        return IsRaycastHit(target,self, minMaxRange, layerMask, out float distance);
    }

    private bool IsRaycastHit(Transform target, Transform self, Vector2 minMaxRange, LayerMask layerMask, out float distance) {
        var dir = target.position - self.position;
        if (Physics.Raycast(self.position, dir, out RaycastHit hit, minMaxRange.y, layerMask)) //todo: use eye position instead
        {
            distance = hit.distance;
            return hit.transform == target && hit.distance > minMaxRange.x;
        }
        distance = 0;
        return false;
    }
}