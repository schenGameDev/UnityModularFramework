using System;
using System.Linq;
using ModularFramework;
using static ModularFramework.Utility.MathUtil;
using EditorAttributes;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

public class VisionBlocker : Marker {
    [SerializeField] private bool _static = false;
    [SerializeField] private Vector2 _minMaxHeight;
    // [SerializeField] private bool _meshChange = false;
    [SerializeField] private Vector2[] _vertices;

    private Vector2 _initialPos;

    public VisionBlocker() {
        RegistryTypes = new[] { new[] {typeof(ZipVisionMap), typeof(PointVisionMap),typeof(PolygonVisionMap)}};
    }

    protected override void Start() {
        base.Start();
        _initialPos = new(transform.position.x, transform.position.z);
    }

    public IEnumerable<Vector2> GetVertices(float viewHeight) {
        if(!_static){
            var minMaxH = MoveHeight();
            if(WithinRange(viewHeight, minMaxH.x, minMaxH.y)) {
                return MoveVertices();
            }
        }

        if(_vertices != null && _vertices.Length > 0 &&
           WithinRange(viewHeight, _minMaxHeight.x, _minMaxHeight.y)) {
            return _vertices;
        }

        return null;
    }

    [Button]
    public void GetVerticesFromMesh() {
        var mf = GetComponent<MeshFilter>();
        if(mf == null) return;
        var points = mf.sharedMesh.vertices.Select(v=>transform.TransformPoint(v));
        float minh = float.MaxValue, maxh = float.MinValue;
        var vs = new List<Vector2>();
        foreach(var p in points) {
            if(p.y > maxh) maxh=p.y;
            if(p.y < minh) minh=p.y;
            var v = new Vector2(p.x,p.z);
            if(!vs.Contains(v)) vs.Add(v);
        }
        _minMaxHeight.x = minh;
        _minMaxHeight.y = maxh;
        _vertices = vs.ToArray();
    }

    private Vector2 MoveHeight() {
        var dir = transform.position.y- _initialPos.y;
        return new Vector2(_minMaxHeight.x + dir, _minMaxHeight.y + dir);
    }

    private IEnumerable<Vector2> MoveVertices() {
        Vector2 dir = new(transform.position.x- _initialPos.x, transform.position.z- _initialPos.y);
        return _vertices.Select(v=>v + dir);
    }

    private void OnDrawGizmosSelected() {
        if(_vertices == null) return;
        int i = 0;
        Vector3 textPos = new Vector3(0.1f,0,-0.1f);
        foreach(var v in _vertices) {
            var verticePos = new Vector3(v.x,_minMaxHeight.x, v.y);
            Gizmos.DrawSphere(verticePos, 0.1f);
            GUIStyle text = new GUIStyle();
            text.fontStyle = FontStyle.Normal;
            text.fontSize = 12;
            text.normal.textColor = Color.red;
            Handles.Label(verticePos + textPos, (++i).ToString(), text);
        }
    }
}