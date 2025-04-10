using System.Collections.Generic;
using Polygon2D;
using static ModularFramework.Utility.GeometryUtil;
using static ModularFramework.Utility.MathUtil;
using static Polygon2D.Utility;
using UnityEngine;
using System.Linq;
using UnityEngine.ProBuilder;
using System;
using ModularFramework;
using ModularFramework.Utility;
using UnityEngine.ProBuilder.MeshOperations;
using Edge = Polygon2D.Edge;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "VisionMap_SO", menuName = "Game Module/Zip Vision Map")]
public class ZipVisionMap : VisionMap
{
    #region Generic
    [SerializeField] Material _curtainMaterial;
    [SerializeField] private int _maxTracePointNum = 500;
    [RuntimeObject] private Polygon _visionPolygon;
    [RuntimeObject] private List<Polygon> _holePolygons = new();
    [RuntimeObject] private List<TracePoint> _trace = new();
    private TracePoint LastPoint => _trace.IsEmpty()? null : _trace[^1];
    private bool _isVisionPolygonChanged = false;
    [SerializeField] float _minHoleLen = 0.3f;
    private float _minHoleSize;


    protected override void CalculateVision()
    {

        _trace.Add(new TracePoint(GetPlayerPosition(), GetCurrentViewAngle(),
                                  Round(ConeVisionDistance / stepSize), Round(PeripheralVisionRadius / stepSize),
                                  ConeAngle / 2, LastPoint));
        if(_trace.Count==1) {
            // all
            _visionPolygon = GetViewPolygon(LastPoint);
            _isVisionPolygonChanged = true;
        } else if(_trace[^2] != LastPoint) {
            // turn & move
            Polygon newPol = GetRotateMovePolygon(_trace[^2], LastPoint);
            if(newPol == null || !newPol.IsValid) return;
            // remove holes
            _holePolygons = _holePolygons.Select(hole => Cut(hole, newPol)).Where(hole => hole.Count > 0).Select(hole => hole[0]).ToList();

            var pols = MergeConnectedPolygons(_visionPolygon, newPol, true);
            if(pols!=null && pols.NonEmpty()) _visionPolygon = pols[0];
            pols.Skip(1).Where(hole => hole.IsValid && hole.BoundBox.height * hole.BoundBox.width > _minHoleSize).ForEach(hole => _holePolygons.Add(hole));
            _isVisionPolygonChanged = true;
        } else {
            _isVisionPolygonChanged = false;
        }

        if(_trace.Count > _maxTracePointNum) _trace.RemoveAt(0);

    }

    protected override void ImplementVision()
    {
        if(_isVisionPolygonChanged) {
            UpdatePolygonCurtain();
            UpdateBlockersAndHoles();
        }
    }

    protected override void Prepare()
    {
        CreateProMeshCurtain();
        SetUpPool();
    }

    protected override void Reset() {
        base.Reset();
        _minHoleSize = Mathf.Pow(_minHoleLen / stepSize, 2);
        GetWorldBounds();
    }

    public override void OnGizmos() {}


    #endregion
    #region Trace

    class TracePoint {
        public Vector2Int Center;
        public int R;
        public int r;
        public int HalfConeAngle;
        public int ViewDirection; // related to z
        public Vector2Int Movement;
        public TracePoint(Vector2Int center, int viewAngle, int R, int r, int halfConeAngle) {
            this.R = R;
            this.r = r;
            HalfConeAngle = halfConeAngle;
            Center = center;
            ViewDirection = viewAngle < 0 ? viewAngle + 360 : viewAngle;
            Movement = Vector2Int.zero;
        }

        public TracePoint(Vector2Int center, int viewAngle, int R, int r, int halfConeAngle, TracePoint lastTracePoint) {
            this.R = R;
            this.r = r;
            HalfConeAngle = halfConeAngle;
            Center = center;
            ViewDirection =  viewAngle < 0 ? viewAngle + 360 : viewAngle;

            if(lastTracePoint == null) return;

            Movement = center - lastTracePoint.Center;
        }

        public bool IsStationary() => SamePoint(Movement,Vector2.zero);
        public bool IsCircle() => R == r || HalfConeAngle >= 180;

        public static bool operator ==(TracePoint lhs, TracePoint rhs) {
            if (lhs is null)
            {
                return rhs is null;
            }
            return lhs.Equals(rhs);
        }

        public static bool operator !=(TracePoint lhs, TracePoint rhs) => !(lhs == rhs);

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            TracePoint other = obj as TracePoint;
            return Center == other.Center && ViewDirection == other.ViewDirection;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Center, ViewDirection);
        }

        public bool IsParameterEqual(TracePoint other) {
            return R == other.R && r == other.r && HalfConeAngle == other.HalfConeAngle;
        }
    }


    #endregion
    #region Snapshot

    private int GetCurrentViewAngle() {
        int viewAngleRelativeToZ = NormalizeAngle((int) Player.GetComponent<IViewer>().ViewAngle);
        // viewAngleRelativeToZ -= viewAngleRelativeToZ % 15;
        return  viewAngleRelativeToZ;
    }

    private Vector2Int GetPlayerPosition() => NearestPoint(Player.position);
    private Polygon GetViewPolygon(TracePoint point) {
        Polygon polygon;
        if(point.IsCircle()){
            polygon = new Polygon(CreateCircle(point.R).Select(x=>x + point.Center).ToList(), false);
        } else {
            int left = point.ViewDirection - point.HalfConeAngle;
            int right = point.ViewDirection + point.HalfConeAngle;
            polygon = new Polygon(CreateCircleArc(left,right,point.R,point.r).Select(x=>x + point.Center).ToList(), false);
        }

        return CutWorldBounds(polygon);
    }

    // private Polygon GetRotatePolygon(TracePoint point, int viewDirection) {
    //     int lastDirection = point.ViewDirection;

    //     bool turnClockwise = AngleDifference(lastDirection, viewDirection) > 0;

    //     int left = turnClockwise? lastDirection : viewDirection - point.HalfConeAngle;
    //     int right = turnClockwise? viewDirection + point.HalfConeAngle : lastDirection;

    //     return CutWorldBounds(new Polygon(CreateArc(left,right,point.R).Select(x=>x + point.Center).ToList(), false));
    // }

    private Polygon RotateCircleArc(TracePoint lastPoint, TracePoint point) {
        int lastDirection = lastPoint.ViewDirection;
        int thisDirection = point.ViewDirection;

        bool turnClockwise = AngleDifference(lastDirection, thisDirection) > 0;

        if(lastPoint.IsParameterEqual(point)) {
            if(lastDirection == thisDirection) return null;
            return new Polygon(CreateArc(turnClockwise? lastDirection : thisDirection - point.HalfConeAngle,
                                        turnClockwise? thisDirection + point.HalfConeAngle : lastDirection,point.R).Select(x=>x + point.Center).ToList(), false);
        }

        int lastHalfCone = lastPoint.HalfConeAngle;
        int thisHalfCone = point.HalfConeAngle;
        //int halfConeDegDiff = thisHalfCone - lastHalfCone;

        int left,right;

        if(lastPoint.R < point.R) { // be longer
            left = turnClockwise? lastDirection - thisHalfCone : thisDirection - thisHalfCone;
            right = turnClockwise? thisDirection + thisHalfCone : lastDirection + thisHalfCone;
        } else { // be shorter
            if(lastHalfCone >= thisHalfCone) {
                left = turnClockwise? lastDirection : thisDirection - thisHalfCone;
                right = turnClockwise? thisDirection + thisHalfCone : lastDirection;
            } else {
                left = turnClockwise? lastDirection - thisHalfCone : thisDirection - thisHalfCone;
                right = turnClockwise? thisDirection + thisHalfCone : lastDirection + thisHalfCone;
            }

        }

        if(lastPoint.r >= point.r) { // create arc only
            return new Polygon(CreateArc(left,right,point.R).Select(x=>x + point.Center).ToList(), false);
        } else { // create circle & arc
            return new Polygon(CreateCircleArc(left,right,point.R,point.r).Select(x=>x + point.Center).ToList(), false);
        }

    }

    private Polygon GetRotatePolygon(TracePoint lastPoint, TracePoint point) {
        if(lastPoint.r >= point.R) return null;

        if(point.IsCircle()) {
            return new Polygon(CreateCircle(point.R).Select(x=>x + point.Center).ToList(), false);
        }
        if(lastPoint.IsCircle()) {
            if(lastPoint.R < point.r) {
                return GetViewPolygon(point);
            }
            return new Polygon(CreateArc(point.ViewDirection-point.HalfConeAngle,
                                        point.ViewDirection+point.HalfConeAngle,
                                        point.R).Select(x=>x + point.Center).ToList(), false);
        }

        return RotateCircleArc(lastPoint, point);
    }

    private Polygon GetRotateMovePolygon(TracePoint lastPoint, TracePoint point) {

        if(point.IsStationary()) return CutWorldBounds(GetRotatePolygon(lastPoint, point));

        if(point.IsCircle()) {
            return CutWorldBounds(ParallelMove(new Polygon(CreateCircle(point.R).Select(x=>x + point.Center).ToList(), false),point.Movement));
        }
        if(lastPoint.IsCircle()) { // circle to circleArc
            return CutWorldBounds(ParallelMove(new Polygon(CreateCircleArc(point.ViewDirection-point.HalfConeAngle,
                                                                            point.ViewDirection+point.HalfConeAngle,
                                                                            point.R,point.r).Select(x=>x + point.Center).ToList(), false), point.Movement));
        }

        // if same-position point link dont cross polygon keep it

        Polygon rotPolygon = GetRotatePolygon(lastPoint, point);
        Polygon newPolygon = new Polygon(CreateCircleArc(point.ViewDirection-point.HalfConeAngle,
                                                        point.ViewDirection+point.HalfConeAngle,
                                                        point.R,point.r).Select(x=>x + point.Center).ToList(), false);
        Polygon movedPolygon = ParallelMove(newPolygon,point.Movement);

        if(rotPolygon == null || !rotPolygon.IsValid) return CutWorldBounds(movedPolygon);

        return CutWorldBounds(MergeConnectedPolygons(movedPolygon, rotPolygon,false)[0]);

        // if(lastPoint.IsParameterEqual(point)) {
        //     int lastDirection = lastPoint.ViewDirection;
        //     int curDirection = point.ViewDirection;

        //     int degs = AngleDifference(lastDirection, curDirection);
        //     bool turnClockwise = degs > 0;
        //     degs = math.abs(degs);
        //     bool overlap = degs < point.HalfConeAngle;

        //     int leftA = lastDirection - point.HalfConeAngle;
        //     int rightA = lastDirection + point.HalfConeAngle;
        //     int leftB = curDirection - point.HalfConeAngle;
        //     int rightB = curDirection + point.HalfConeAngle;

        //     int left = turnClockwise? (overlap? leftB : rightA) : leftB;
        //     int right = turnClockwise? rightB : (overlap? rightB : leftA);

        //     return CutWorldBounds(ParallelMove(new Polygon(CreateCircleArc(left,right,point.R,point.r).Select(x=>x + point.Center).ToList(), false), point.Movement));
        // }

        // return null;

    }


    private static uint AddBoundsHit(Vector2 point, float halfX, float halfY) {
        // up = 1 , down = 2, left =4, right =8
        uint hits = 0;
        if(point.y> halfY) hits+=1;
        if(point.y< -halfY) hits+=2;
        if(point.x< -halfX) hits+=4;
        if(point.x> halfX) hits+=8;

        return hits;
    }

    #endregion

#region Blocker & Hole
    [RuntimeObject(nameof(SetUpPool))] private ObjectPool<ProBuilderMesh> _extraShadowPool;
    [RuntimeObject] private List<ProBuilderMesh> _activeShadows = new();

    private void SetUpPool() {
        _extraShadowPool = new ObjectPool<ProBuilderMesh>(
            createFunc: () => {
                GameObject shadow = new();
                shadow.transform.position = new Vector3(center.x,mapHeight + 1,center.y);
                var mr = shadow.AddComponent<MeshRenderer>();
                mr.material = _curtainMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                shadow.transform.parent = maskParent;
                shadow.name = "Hole";
                shadow.SetActive(false);
                return shadow.AddComponent<ProBuilderMesh>();
            },
            actionOnGet: shadow => shadow.gameObject.SetActive(true),
            actionOnRelease: shadow => shadow.gameObject.SetActive(false),
            actionOnDestroy: shadow => Destroy(shadow.gameObject),
            defaultCapacity: 40,
            maxSize: 80
        );
    }
    private void UpdateBlockersAndHoles() {
        if(_trace.IsEmpty()) return;
        List<Polygon> blockerAndHoles = new(GetBlockers(GetPlayerPosition(), LastPoint.R));
        blockerAndHoles.AddRange(_holePolygons);
        // pooling, nativeArray to update mesh of object
        while(_activeShadows.Count < blockerAndHoles.Count) {
            _activeShadows.Add(_extraShadowPool.Get());
        }
        while(_activeShadows.Count > blockerAndHoles.Count) {
            _extraShadowPool.Release(_activeShadows.Pop());
        }
        int count = 0;
        var l = blockerAndHoles.Select(x=>(x,count++)).ToList();
        l.ForEach(e => {
            var polygon = e.Item1;
            var idx = e.Item2;
            List<Vector3> index = GenerateMeshIndex(polygon.Vertices);
            var proMesh = _activeShadows[idx];
            var result = proMesh.CreateShapeFromPolygon(index, 0, false);
            if(result.status != ActionResult.Status.Success) {
                DebugUtil.Error(result.status + ":" + result.notification + ": " + polygon);
            }
        });
    }

#endregion
#region Curtain
    Vector3[] _maskVertices;
    ProBuilderMesh _proCurtainMesh;

    private void CreateProMeshCurtain() {
        var curtain = new GameObject();
        curtain.transform.position = new Vector3(center.x,mapHeight,center.y);
        var mr = curtain.AddComponent<MeshRenderer>();
        mr.material = _curtainMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        _proCurtainMesh = curtain.AddComponent<ProBuilderMesh>();
        curtain.transform.parent = maskParent;
        curtain.name = "Main";

        var maskHalfX = dimension.x/2f +1;
        var maskHalfY = dimension.y/2f +1;

        _maskVertices = new[]  {new Vector3(-maskHalfX,0,-maskHalfY),
                                new Vector3(-maskHalfX,0,maskHalfY),
                                new Vector3(maskHalfX,0,maskHalfY),
                                new Vector3(maskHalfX,0,-maskHalfY)};
    }
    private void UpdatePolygonCurtain() {
        if(_trace.IsEmpty() || _visionPolygon==null || !_visionPolygon.IsValid) return;
        List<IList<Vector3>> visibleRegions = GenerateHoleIndex(new() {_visionPolygon});
        var result = _proCurtainMesh.CreateShapeFromPolygon(_maskVertices, 0, false, visibleRegions);
        if(result.status != ActionResult.Status.Success) {
            DebugUtil.Error(result.status + ":" + result.notification + ": " + _visionPolygon);
        }


    }
    private List<IList<Vector3>> GenerateHoleIndex(List<Polygon> polygons) {
        List<IList<Vector3>> index = new();
        polygons.Select(polygon => GenerateMeshIndex(polygon.Vertices))
                        .ForEach(i=>index.Add(i));
        return index;
    }

    private List<Vector3> GenerateMeshIndex(List<Vector2> vertices) {
        return vertices.Select(v=>TranslateToRelativeWorldPoint(v)).ToList();
    }

#endregion

    #region World Bound
    private float _halfX, _halfY;
    private Polygon[] _worldBounds;
    private void GetWorldBounds() {
        _halfX = Round(dimension.x/2/ stepSize);
        _halfY = Round(dimension.y/2/ stepSize);

        _worldBounds = new Polygon[] {
                new Polygon(new() { new(-_halfX, _halfY), new(-_halfX, 2 * _halfY), new(_halfX, 2 * _halfY), new(_halfX, _halfY) }),
                new Polygon(new() { new(-_halfX, -_halfY), new(-_halfX, -2 * _halfY), new(_halfX, -2 * _halfY), new(_halfX, -_halfY) }),
                new Polygon(new() { new(-_halfX, _halfY), new(-2 * _halfX, _halfY), new(-2 * _halfX, -_halfY), new(-_halfX, -_halfY) }),
                new Polygon(new() { new(_halfX, _halfY), new(2 * _halfX, _halfY), new(2 * _halfX, -_halfY), new(_halfX, -_halfY) })
            };
    }

    private Polygon CutWorldBounds(Polygon polygon) {
        if(polygon == null || !polygon.IsValid) return null;
        uint boundHits = 0;
        polygon.Vertices.ForEach(v=>boundHits |= AddBoundsHit(v, _halfX, _halfY));

        if(boundHits>0) {
            if((boundHits & 1) == 1) polygon = Cut(polygon, _worldBounds[0])[0];
            if((boundHits & 2) == 2) polygon = Cut(polygon, _worldBounds[1])[0];
            if((boundHits & 4) == 4) polygon = Cut(polygon, _worldBounds[2])[0];
            if((boundHits & 8) == 8) polygon = Cut(polygon, _worldBounds[3])[0];
        }

        return polygon;
    }
    #endregion

    #region ParallelMove
    public static Polygon ParallelMove(Polygon polygon, Vector2 displacement) {
        // find front points
        List<Vector2> points = polygon.Vertices;
        List<Edge> edges = polygon.Edges;
        List<Vector2> frontPoints = new();
        foreach(Vector2 point in points) {
            Vector2 b = point + displacement * 100;
            bool noIntersection = true;
            Vector2 intersection = Vector2.zero;
            foreach(Edge e in edges) {
                if(e.A == point || e.B == point) continue;

                if(LineSegmentIntersection(point, b, e.A, e.B, ref intersection)) {
                    noIntersection = false;
                    break;
                }
            }
            if(noIntersection) frontPoints.Add(point);
        }

        List<Edge> frontEdges = new();
        List<Edge> backEdges = new();
        foreach(Edge e in edges) {
            if(frontPoints.Contains(e.A) && frontPoints.Contains(e.B)) {
                frontEdges.Add(e);
            } else {
                backEdges.Add(e);
            }
        }
        frontEdges = RearrangeEdges(frontEdges);
        backEdges = RearrangeEdges(backEdges);

        List<Vector2> newPoints = new() {backEdges[0].A};
        foreach(Edge e in backEdges) {
            newPoints.Add(e.B);
        }

        if(frontEdges[0].A != backEdges[^1].B) frontEdges.Reverse();
        newPoints.Add(frontEdges[0].A + displacement);
        foreach(Edge e in frontEdges) {
            newPoints.Add(e.B + displacement);
        }

        return new Polygon(newPoints, false);
    }

    public static List<Vector2> FindFrontPointsInParallelMove(Polygon polygon, Vector2 displacement) {
        // find front points
        List<Vector2> points = polygon.Vertices;
        List<Edge> edges = polygon.Edges;
        List<Vector2> frontPoints = new();
        foreach(Vector2 point in points) {
            Vector2 b = point + displacement * 100;
            bool noIntersection = true;
            Vector2 intersection = Vector2.zero;
            foreach(Edge e in edges) {
                if(e.A == point || e.B == point) continue;

                if(LineSegmentIntersection(point, b, e.A, e.B, ref intersection)) {
                    noIntersection = false;
                    break;
                }
            }
            if(noIntersection) frontPoints.Add(point);
        }

        List<Edge> frontEdges = new();
        foreach(Edge e in edges) {
            if(frontPoints.Contains(e.A) && frontPoints.Contains(e.B)) {
                frontEdges.Add(e);
            }
        }
        frontEdges = RearrangeEdges(frontEdges);
        List<Vector2> newPoints = new() {frontEdges[0].A};
        foreach(Edge e in frontEdges) {
            newPoints.Add(e.B);
        }
        return newPoints;
    }

    private static List<Edge> RearrangeEdges(List<Edge> edges) {
        List<List<Edge>> segments = new();
        List<Edge> temp = new();
        foreach(Edge v in edges) {
            if(temp.IsEmpty() || (temp[^1].B == v.A)) {
                temp.Add(v);
            } else {
                segments.Add(temp);
                temp = new() {v};
            }
        }
        if(temp.NonEmpty()) segments.Add(temp);

        while(segments.Count>1) {
            var f = segments.Pop();
            segments.ForEach(s=>{
                if(s[^1].B == f[0].A) s.AddRange(f);
                else if(s[0].A==f[^1].B) s.InsertRange(0,f);
            });
        }
        return segments[0];
    }
    #endregion

    #region  Util
    [RuntimeObject] Dictionary<int,CircularList<Vector2>> _circlePoints = new();
    private List<Vector2> CreateArc(int fromAngle, int toAngle, int R) { // clockwise
        CircularList<Vector2> circlePoints;
        if(!_circlePoints.TryGetValue(R, out circlePoints)) {
            circlePoints = CreateCircle(R);
        }

        fromAngle = NormalizeAngle(fromAngle);
        toAngle = NormalizeAngle(toAngle);

        if(fromAngle == toAngle) return circlePoints;
        List<Vector2> points = GetArc(circlePoints,fromAngle, toAngle);
        points.Insert(0, Vector2.zero);
        return points;
    }

    private List<Vector2> CreateArc(int fromAngle, int crossAngle, int toAngle, int fromR, int toR) { // clockwise
        CircularList<Vector2> circlePoints;
        if(!_circlePoints.TryGetValue(toR, out circlePoints)) {
            circlePoints = CreateCircle(toR);
        }

        fromAngle = NormalizeAngle(fromAngle);
        toAngle = NormalizeAngle(toAngle);

        if(fromAngle == toAngle) return circlePoints;
        List<Vector2> points = GetArc(circlePoints,fromAngle, toAngle);
        points.Insert(0, Vector2.zero);
        return points;
    }

    private List<Vector2> CreateCircleArc(int fromAngle, int toAngle, int R, int r) { // clockwise
        CircularList<Vector2> circlePoints, circlePointsSmall;
        if(!_circlePoints.TryGetValue(R, out circlePoints)) {
            circlePoints = CreateCircle(R);
        }
        if(!_circlePoints.TryGetValue(r, out circlePointsSmall)) {
            circlePointsSmall = CreateCircle(r);
        }

        fromAngle = NormalizeAngle(fromAngle);
        toAngle = NormalizeAngle(toAngle);

        if(fromAngle == toAngle) return circlePoints;
        List<Vector2> points = GetArc(circlePoints,fromAngle, toAngle);
        List<Vector2> pointsSmall = GetArc(circlePointsSmall,toAngle, fromAngle);
        points.AddRange(pointsSmall);
        return points;
    }

    private List<Vector2> GetArc(CircularList<Vector2> clist, int start, int end) {
        bool isLoop = start > end;
        List<Vector2> list = new();
        int e = isLoop? (clist.Count-1) : end;
        for(int i = start; i<=e; i++) {
            if(i==start || i%10 == 0)
                list.Add(clist[i]);
        }

        if(isLoop) {
            for(int i = 0; i<=end; i++) {
                if(i==end || i%10 == 0)
                    list.Add(clist[i]);
            }
        }
        return list;
    }
    private CircularList<Vector2> CreateCircle(int R) {
        CircularList<Vector2> points = new();
        Vector2Int vec = new Vector2Int(0,R);
        for (int i = 0; i<360; i+= 10) {
            Vector2 temp = Rotate(vec, i * Mathf.Deg2Rad);
            points.Add(Round(temp));
        }
        for (int t = 0; t<360; t+=10) {
            Vector2 s = points[t];
            Vector2 e = points[t+1>=points.Count? 0 : t+1];
            Vector2 vector = e - s;
            vector.x = RoundTo(vector.x / 10, 2);
            vector.y = RoundTo(vector.y / 10, 2);

            Vector2 temp = vector;

            for(int j=0; j<9; j++) {
                points.Insert(t + j + 1, s + temp);
                temp += vector;
            }
        }
        // DebugUtil.DebugLog(points);
        _circlePoints.Add(R, points);
        return points;
    }

    #endregion
}