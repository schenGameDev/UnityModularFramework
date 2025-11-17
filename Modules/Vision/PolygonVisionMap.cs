
using System.Collections.Generic;
using ModularFramework.Utility;
using static ModularFramework.Utility.GeometryUtil;
using static ModularFramework.Utility.MathUtil;
using UnityEngine;
using System.Linq;
using EditorAttributes;
using Polygon2D;

using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using static Polygon2D.Utility;
using Math = System.Math;
using System.Threading.Tasks;
using Unity.Collections;
using System.Collections.Concurrent;
using System;
using ModularFramework;
using UnityEngine.Pool;
using Unity.Jobs;
using Unity.Burst;


[CreateAssetMenu(fileName ="VisionMap_SO",menuName ="Game Module/Polygon Vision Map")]
public class PolygonVisionMap : VisionMap
{
    [Header("Curtain")]
    [SerializeField] float _minSmoothRoom = 1;
    [SerializeField] Material _curtainMaterial;
    [SerializeField, ShowField(nameof(isDim)), Suffix("step (Dividable by 2)")] protected int _circleRadius = 10;
    [SerializeField] bool _showGizmos;

    protected override void Prepare()
    {
        CreateProMeshCurtain();
    }


    protected override void Reset() {
        base.Reset();
        _mergedPolygon = new();
        _holePolygon = new();
        _dimCircleDict = new();
        _outlineCircleDict = new();
        _halfX = Round(dimension.x/2/ stepSize);
        _halfY = Round(dimension.y/2/ stepSize);
        _r = Round(PeripheralVisionRadius / stepSize);
        _R = Round(ConeVisionDistance / stepSize);
        _halfConeAngle = ConeAngle / 2;

        _worldBounds = new Polygon[] {
                new Polygon(new() { new(-_halfX, _halfY), new(-_halfX, 2 * _halfY), new(_halfX, 2 * _halfY), new(_halfX, _halfY) }),
                new Polygon(new() { new(-_halfX, -_halfY), new(-_halfX, -2 * _halfY), new(_halfX, -2 * _halfY), new(_halfX, -_halfY) }),
                new Polygon(new() { new(-_halfX, _halfY), new(-2 * _halfX, _halfY), new(-2 * _halfX, -_halfY), new(-_halfX, -_halfY) }),
                new Polygon(new() { new(_halfX, _halfY), new(2 * _halfX, _halfY), new(2 * _halfX, -_halfY), new(_halfX, -_halfY) })
            };

        _circleOffsets = new();
        _extraFullDimsLastFrame = new();
        SetUpPool();
    }

    private List<Polygon> _mergedPolygon;
    private List<Polygon> _holePolygon;
    private Polygon _curVision;
    protected override void CalculateVision()
    {
        _curVision = GetCurrentViewPolygon();
        // latest vision
        HashSet<Vector2Int> newCircleTriangularPositions = AddCirclesInCurrentVision(_curVision);
        // dim
        ConcurrentDictionary<Vector2Int, DimProfile> dimProfileDict = isDim?
            Dim(newCircleTriangularPositions) : new();

        // find holes first
        _holePolygon = GetHoles(dimProfileDict);

        if(_isTest) _log = String.Join(";",dimProfileDict.Select(e=>"("+e.Key + ":"+e.Value+")"));
        List<Polygon> dimsFromOuterEdge = MergeDimPolygons(dimProfileDict);

        if(dimsFromOuterEdge.NonEmpty()) {
            List<Polygon> afterCut = new();
            if(_mergedPolygon.Count > 1) {
                foreach(var x in _mergedPolygon.AsParallel().Select(pol => Cut(pol, new List<Polygon>(dimsFromOuterEdge))).ToList()) {
                    afterCut.AddRange(x);
                }
            } else if(_mergedPolygon.NonEmpty()) {
                List<Polygon> ts = Cut(_mergedPolygon[0], dimsFromOuterEdge);
                afterCut.AddRange(ts);
            }

            _mergedPolygon = afterCut;
        }

        if(_mergedPolygon.IsEmpty()) _mergedPolygon.Add(_curVision);
        else {
            _mergedPolygon = Merge(_mergedPolygon, _curVision);
        }

        if(_holePolygon.NonEmpty()) {
            _holePolygon.RemoveWhere(h => {
                for(int i = 0; i< _mergedPolygon.Count; i+=1) {
                    var m = _mergedPolygon[i];
                    var after = Cut(m, h);
                    if(after.Count == 1 && after[0]!=m) {
                        _mergedPolygon[i] = after[0];
                        return true;
                    }
                }
                return false;
            });

        }

        if(_mergedPolygon.Count > 10) {
            DebugUtil.Warn("too many polygons");
        } else if(_mergedPolygon.IsEmpty()) {
            DebugUtil.Error("Empty merged polygon");
        }
    }


    protected override void ImplementVision()
    {
        // float minTriangleArea = _minSmoothRoom / _stepSize / _stepSize;
        // _mergedPolygon.Compute(polygon => Smooth(polygon, minTriangleArea));
        // _holePolygon.Compute(polygon => Smooth(polygon, minTriangleArea));

        // _dimCircleDict.RemoveIfValue(p => {
        //     foreach(Polygon pol in _mergedPolygon) {
        //         var b = pol.PointInPolygon(p.CartesianPosition);
        //         if(!b.HasValue || b.Value) {
        //             return false;
        //         }
        //     }
        //     return true;
        // });

        UpdatePolygonCurtain();
        UpdateBlockersAndHoles();
    }

    protected override void OnDraw()
    {
        if(!_showGizmos) return;
        Gizmos.color = Color.yellow;
        foreach(var polygon in _mergedPolygon) {
            for(int i = 1;i<polygon.Vertices.Count; i++) {
                Gizmos.DrawLine(TranslateToWorldPoint(Round(polygon.Vertices[i-1]), 1), TranslateToWorldPoint(Round(polygon.Vertices[i]), 1));
            }
            Gizmos.DrawLine(TranslateToWorldPoint(Round(polygon.Vertices[^1]), 1), TranslateToWorldPoint(Round(polygon.Vertices[0]), 1));
        }

        if(_curVision!=null){
            Gizmos.color = Color.blue;
            for(int i = 1;i<_curVision.Vertices.Count; i++) {
                Gizmos.DrawLine(TranslateToWorldPoint(Round(_curVision.Vertices[i-1]), 1), TranslateToWorldPoint(Round(_curVision.Vertices[i]), 1));

            }
            Gizmos.DrawLine(TranslateToWorldPoint(Round(_curVision.Vertices[^1]), 1), TranslateToWorldPoint(Round(_curVision.Vertices[0]), 1));
        }

        Gizmos.color = Color.red;
        foreach(var v in _outlineCircleDict.Keys) {
            Gizmos.DrawSphere(TranslateToWorldPoint(Round(ConvertTriangularToCartesianCoord(v)), 1), 0.1f);
        }

        Gizmos.color = Color.green;
        foreach(var v in _dimCircleDict.Keys) {
            Gizmos.DrawSphere(TranslateToWorldPoint(Round(ConvertTriangularToCartesianCoord(v)), 1), 0.1f);
        }
    }

#region Visible Region
    private int _r, _R, _halfConeAngle;
    private float _halfX, _halfY;
    private Vector2 _viewDirection;
    private Vector2Int _visionCenter;
    private Polygon[] _worldBounds;
    private Polygon GetCurrentViewPolygon() {
        List<Vector2> points = new();

        var viewAngleRelativeToZ = Player.GetComponent<IViewer>().ViewAngle;
        _viewDirection =  Rotate(Vector2.up, viewAngleRelativeToZ);
        _visionCenter = NearestPoint(Player.position);

        Vector2Int vec;
        Vector2 temp;
        HashSet<int> boundsHit = new(); // left = 0
        for (int i = 0; i<=180; i+= 10) {
            if(_halfConeAngle==i) {
                vec = new Vector2Int(0,_R);
                temp = _visionCenter + Rotate(vec, (viewAngleRelativeToZ + i) * Mathf.Deg2Rad);
                points.Add(Round(temp));
                AddBoundsHit(temp, ref boundsHit);
                temp = _visionCenter + Rotate(vec, (viewAngleRelativeToZ - i) * Mathf.Deg2Rad);
                points.Insert(0, Round(temp));
                AddBoundsHit(temp, ref boundsHit);

                vec = new Vector2Int(0,_r);
                temp = _visionCenter + Rotate(vec, (viewAngleRelativeToZ +i) * Mathf.Deg2Rad);
                points.Add(Round(temp));
                AddBoundsHit(temp, ref boundsHit);
                temp = _visionCenter + Rotate(vec, (viewAngleRelativeToZ - i) * Mathf.Deg2Rad);
                points.Insert(0, Round(temp));
                AddBoundsHit(temp, ref boundsHit);
            } else {
                vec = new Vector2Int(0,i<=_halfConeAngle? _R:_r);
                temp = _visionCenter + Rotate(vec, (viewAngleRelativeToZ +i) * Mathf.Deg2Rad);
                points.Add(Round(temp));
                AddBoundsHit(temp, ref boundsHit);
                if(i!=0 && i!= 180) {
                    temp = _visionCenter + Rotate(vec, (viewAngleRelativeToZ - i) * Mathf.Deg2Rad);
                    points.Insert(0, Round(temp));
                    AddBoundsHit(temp, ref boundsHit);
                }
            }
        }

        var polygon = new Polygon(points);
        if(boundsHit.NonEmpty()) polygon = Cut(polygon, boundsHit.Select(b => _worldBounds[b]).ToList())[0];
        return polygon;

    }

    private void AddBoundsHit (Vector2 point, ref HashSet<int> hits) {
        // up = 0 , down = 1, left =2, right =3
        if(point.x< -_halfX) hits.Add(2);
        if(point.x> _halfX) hits.Add(3);
        if(point.y> _halfY) hits.Add(0);
        if(point.y< -_halfY) hits.Add(1);
    }

    private Vector2 Rotate(Vector2 v, float delta) {
        return new Vector2(
            v.x * Mathf.Cos(delta) - v.y * Mathf.Sin(delta),
            v.x * Mathf.Sin(delta) + v.y * Mathf.Cos(delta)
        );
    }

    private bool CleanPoints(HashSet<Vector2Int>  circleInCurVision) {
        var hasMerged = !_mergedPolygon.IsEmpty();
        List<Polygon> vision = hasMerged? _mergedPolygon : new() {_curVision};
        ConcurrentHashSet<Polygon> polygonsContainPoint = new();
        ConcurrentHashSet<Vector2Int> inVision = new();
        bool allInCurVision = true;
        Parallel.ForEach(_dimCircleDict, e => {
            if(circleInCurVision.Contains(e.Key)) {
                inVision.TryAdd(e.Key);
                return;
            }
            allInCurVision = false;
            foreach(Polygon pol in vision) {
                var b = pol.PointInPolygon(e.Value.CartesianPosition);
                if(!b.HasValue || b.Value) {
                    if(hasMerged) polygonsContainPoint.TryAdd(pol);
                    inVision.TryAdd(e.Key);
                }
            }
        });


        if(allInCurVision) return true;

        _dimCircleDict.RemoveWhere((k,v)=> {
            return !inVision.Contains(k) &&
                (v.DimmingProgress==0 || GetTriagularCoordNeighbors(k).None(x => inVision.Contains(x)));
        });

        if(hasMerged) {
            _mergedPolygon.RetainAll(polygonsContainPoint.ToHashSet());
        }
        return false;
    }

    private HashSet<Vector2Int> AddCirclesInCurrentVision(Polygon curVision) {
        Vector2Int[] square = curVision.GetSquareBounds().Select(b=> NearestCirclePosition(Round(b))).ToArray();
        HashSet<Vector2Int> newCircleTriangularPositions = new();
        int l = square[3].x - square[0].x;
        float k = ((float)(square[0].y - square[1].y)) / (square[0].x - square[1].x);
        float b = square[1].y - k * square[1].x;
        for(int j = square[0].y; j <= square[2].y; j += 1) {
            int start = Round((j-b) / k);
            for(int i = start; i <= start + l; i += 1) {
                var p = new Vector2Int(i,j);
                var cartesianPos = Round(ConvertTriangularToCartesianCoord(p),2);
                var isInPolygon = curVision.PointInPolygon(cartesianPos);
                if(!isInPolygon.HasValue || isInPolygon.Value) {
                    _dimCircleDict.AddIfAbsent(p, new Circle(cartesianPos));
                    newCircleTriangularPositions.Add(p);
                }
            }
        }
        return newCircleTriangularPositions;
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
        // _activeShadows = new();

    }
    private void UpdateBlockersAndHoles() {
        List<Polygon> blockerAndHoles = new(GetBlockers(_visionCenter, _R));
        blockerAndHoles.AddRange(_holePolygon);
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

    private List<Polygon> GetHoles(ConcurrentDictionary<Vector2Int, DimProfile> dimProfileDict) {
        HashSet<Vector2Int> outliners = FindOutlineCircles();

        List<(List<Vector2Int>,List<Vector2Int>)> outlinerGroups = GroupTriangularLinePoints(outliners, dimProfileDict.Keys);
        List<(List<Vector2Int>,List<Vector2Int>)> holes = new();

        if(outlinerGroups.Count > 1) {
            List<Rect> bounds = outlinerGroups.AsParallel().Select(g => {
                    int xMin=int.MaxValue,xMax=int.MinValue,yMin=int.MaxValue,yMax=int.MinValue;
                    foreach(Vector2Int i in g.Item1) {
                        xMin = Math.Min(i.x,xMin);
                        xMax = Math.Max(i.x,xMax);
                        yMin = Math.Min(i.y,yMin);
                        yMax = Math.Max(i.y,yMax);
                    }
                    return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                }).ToList();

            for(int i=0;i< bounds.Count;i++) {
                var b = bounds[i];
                bool isInside = false;
                for(int j=0;j< bounds.Count;j++) {
                    if(i==j) continue;
                    var c = bounds[j];
                    if(b.xMin >= c.xMin && b.xMax <= c.xMax && b.yMin >= c.yMin && b.yMax <= c.yMax) {
                        isInside = true;
                        break;
                    }
                    if(b.xMin <= c.xMin && b.xMax >= c.xMax && b.yMin <= c.yMin && b.yMax >= c.yMax) {
                        break;
                    }
                }
                if(isInside) holes.Add(outlinerGroups[i]);
            }
        }

        List<Polygon> polygons = new();
        HashSet<Vector2Int> innerPoints = new();
        holes.ForEach(hs => {
            ConcurrentDictionary<Vector2Int,DimProfile> outlineProfileDict = new();
            List<Vector2> cartesianPoints = new();
            hs.Item1.ForEach(v=>{
                var cp = ConvertTriangularToCartesianCoord(v);
                outlineProfileDict.TryAdd(v,new DimProfile(cp,1));
                cartesianPoints.Add(cp);
            });
            Parallel.ForEach(outlineProfileDict, e => FindConnectingNeighbors(e.Key, e.Value, outlineProfileDict));

            var pols = CheckSelfCross(new Polygon(cartesianPoints));
            if(pols.IsEmpty()) return;
            var hole = pols[0];
            List<List<DimProfile>> nonIntersectingCircles = new();

            foreach(var h in hs.Item2) { // inner dimming circles
                if(dimProfileDict.Remove(h,out var a)) {
                    List<DimProfile> profiles;
                    if(nonIntersectingCircles.IsEmpty()) {
                        profiles = nonIntersectingCircles.AddAndReturn(new());
                    } else {
                        var temp = nonIntersectingCircles
                            .Where(t => t.None(x=>x.ConnectingNeighborDims.Values.Contains(h)))
                            .FirstOrDefault();
                        profiles = temp ?? nonIntersectingCircles.AddAndReturn(new());
                    }
                    profiles.Add(a);
                }
            }


            List<List<DimProfile>> nonIntersectingCircles2 = new();
            outlineProfileDict.ForEach((h,a) => {
                List<DimProfile> profiles;
                    if(nonIntersectingCircles2.IsEmpty()) {
                        profiles = nonIntersectingCircles2.AddAndReturn(new());
                    } else {
                        var temp = nonIntersectingCircles2
                            .Where(t => t.None(x=>x.ConnectingNeighborDims.Values.Contains(h)))
                            .FirstOrDefault();
                        profiles = temp ?? nonIntersectingCircles2.AddAndReturn(new());
                    }
                    profiles.Add(a);
            });

            nonIntersectingCircles.InsertRange(0, nonIntersectingCircles2);
            nonIntersectingCircles.ForEach(ps => {
                if(ps.IsEmpty()) return;

                var temp = Merge(ps.Select(p=>CreateCirclePolygon(p.CartesianPosition,p.Percent)).ToList(), hole, true)
                .Select(x=>CheckSelfCross(x)).Where(x=>x.NonEmpty())
                .Select(x=>x[0]).OrderByDescending(x=>x.Vertices.Count).ToList();
                if(temp.NonEmpty()) {
                    hole = temp[0];
                }
            });
            polygons.Add(hole);
        });
        return polygons.Where(pol=>pol.IsValid).ToList();
    }

    private readonly int[] _clockwise = new[] {4,1,2,5,0,3};
    private List<(List<Vector2Int>,List<Vector2Int>)> GroupTriangularLinePoints(ICollection<Vector2Int> points,
                                                                                ICollection<Vector2Int> innerPoints) {
        Vector2Int startPoint = new Vector2Int(int.MinValue, int.MinValue);
        Dictionary<Vector2Int, Vector2Int[]> neighborDict = points
                .Select(p=> (p, GetTriagularCoordNeighbors(p).ToArray()))
                .Where(tup => tup.Item2.Where(v=>points.Contains(v)).Count() > 1)
                .Peek(tup => {
                    if(tup.p.y > startPoint.y) startPoint = tup.p;
                })
                .ToDictionary(tup => tup.p,
                            tup=> tup.Item2);

        List<(List<Vector2Int>,List<Vector2Int>)> groups = new();
        List<Vector2Int> group = new();
        HashSet<Vector2Int> innerLayer = new();
        List<Vector2Int> next = new() {startPoint};

        int prevDirection = 2;
        while(neighborDict.NonEmpty()) {
            Vector2Int point;
            if(next.IsEmpty()) {
                if(group.Count>2) {
                    groups.Add((group,innerLayer.ToList()));
                }
                group = new();
                innerLayer = new();
                point = new Vector2Int(int.MinValue, int.MinValue);
                neighborDict.Keys.ForEach(p => {
                    if(p.y > point.y)point = p;
                });
                prevDirection = 2;
            } else point = next.Pop();

            group.Add(point);
            if(neighborDict.Remove(point, out var neighbors)) {
                int idx = _clockwise.IndexOf(prevDirection) - 2;
                if(idx < 0) idx += _clockwise.Length;
                bool found = false;
                for(int i = 0; i < 6; i+=1) {
                    var p = neighbors[i];
                    if(innerPoints.Contains(p)) innerLayer.Add(p);

                    if(!found && i<5) {
                        if(idx>=_clockwise.Length) idx-=_clockwise.Length;
                        int direction = _clockwise[idx];
                        var n = neighbors[direction];
                        if(n == group[0]) {
                            found = true;
                        } else if(neighborDict.ContainsKey(n)) {
                            next.Add(n);
                            prevDirection = direction;
                            found = true;
                        }
                        idx+=1;
                    }
                }
            }
        }

        if(group.NonEmpty() && group.Count>2) groups.Add((group,innerLayer.ToList()));
        return groups;
    }

    private HashSet<Vector2Int> FindOutlineCircles() {
        HashSet<Vector2Int> outlinePoints = new();
        //HashSet<Vector2Int> innerPoints = new();
        _dimCircleDict.Keys.ForEach(c=>
                    GetTriagularCoordNeighbors(c)
                        .Where(neighbor => !_dimCircleDict.ContainsKey(neighbor))
                        .ForEach(outline => outlinePoints.Add(outline))
            );
        return outlinePoints;
    }

#endregion

#region Curtain
    List<Vector3> _maskVertices;
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

        _maskVertices = new()  {new Vector3(-maskHalfX,0,-maskHalfY),
                                new Vector3(-maskHalfX,0,maskHalfY),
                                new Vector3(maskHalfX,0,maskHalfY),
                                new Vector3(maskHalfX,0,-maskHalfY)};
    }
    private void UpdatePolygonCurtain() {
        List<IList<Vector3>> visibleRegions = GenerateHoleIndex(_mergedPolygon);
        var result = _proCurtainMesh.CreateShapeFromPolygon(_maskVertices, 0, false, visibleRegions);
        if(result.status != ActionResult.Status.Success) {
            DebugUtil.Error(result.status + ":" + result.notification + ": " + String.Join("\n",_mergedPolygon));
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

#region Dim (Circle)
    class Circle {
        public float Timer;
        public bool ReadyToDim;
        public Vector2 CartesianPosition;
        public float DimmingProgress;
        public int Milestone = 1;

        public Circle(Vector2 cartesianPosition) {
            CartesianPosition = cartesianPosition;
        }

        public void Reset() {
            Timer = 0;
            ReadyToDim = false;
            DimmingProgress = 0;
            Milestone = 1;
        }
    }

    class DimProfile {
        public float Percent;
        public Vector2 CartesianPosition;
        public Dictionary<int,Vector2Int> ConnectingNeighborDims;

        public DimProfile(Vector2 cp, float percent) {
            Percent = percent;
            CartesianPosition = cp;
        }

        public override string ToString()
        {
            return Percent + ":" + String.Join(",",ConnectingNeighborDims.Select(e=>"("+e.Key + "," + e.Value+")"));
        }
    }

    private ConcurrentDictionary<Vector2Int,DimProfile> Dim(HashSet<Vector2Int> newCircleTriangularPositions) {
        bool allInCurVision = CleanPoints(newCircleTriangularPositions);
        if(allInCurVision) {
            _mergedPolygon = new() {_curVision};
            RefreshOutlineCircles(newCircleTriangularPositions);
            _dimCircleDict.Values.ForEach(c => c.Reset());
            return new();
        }
        // recalculate outline circles
        Dictionary<Vector2Int,Vector2> outlinersToSubstract = RefreshOutlineCircles(newCircleTriangularPositions);
        // dim
        return GetDimPolygons( newCircleTriangularPositions, outlinersToSubstract);

    }

    Dictionary<Vector2Int,Circle> _dimCircleDict;
    Dictionary<Vector2Int,Circle> _outlineCircleDict;
    HashSet<Vector2Int> _extraFullDimsLastFrame;
    string _log = "";
    private ConcurrentDictionary<Vector2Int,DimProfile> GetDimPolygons(HashSet<Vector2Int> circlesInCurrentVision, Dictionary<Vector2Int,Vector2> extraFullDims)
    {
        // update circle timer
        ConcurrentDictionary<Vector2Int,DimProfile> toDim = new(extraFullDims.Where(x=>!_extraFullDimsLastFrame.Contains(x.Key))
                                                    .ToDictionary(x => x.Key, x=>new DimProfile(x.Value, 1)));
        _extraFullDimsLastFrame = new(extraFullDims.Keys);
        extraFullDims.Clear();
        var toRemove = _dimCircleDict.AsParallel().Where(e => {
            Vector2Int triangularCoordPos = e.Key;
            var circle = e.Value;
            if(circlesInCurrentVision.Contains(triangularCoordPos)) {
                return false;
            }

            Vector2 cartesianPos = circle.CartesianPosition;

            if(circle.ReadyToDim) {
                if(circle.DimmingProgress>0 || AnyNeighborCircleDimmed(triangularCoordPos)) { // propagate circle
                    if(circle.DimmingProgress >= dimTime) {
                        return true;
                    }

                    circle.DimmingProgress = Math.Min(dimTime, circle.DimmingProgress + skippedTime);
                    float percent = circle.DimmingProgress / dimTime;
                    int stage = (int)(percent * 10 + 0.1);
                    if(stage > circle.Milestone) {
                        circle.Milestone = stage;
                        toDim.TryAdd(triangularCoordPos, new (cartesianPos, stage / 10f));
                    }

                }
            } else {
                circle.Timer += skippedTime;
                if(circle.Timer > stayVisibleTime) circle.ReadyToDim = true;
            }
            return false;
        }).Select(e => e.Key).ToList();

        toRemove.ForEach(r => {
            if(_dimCircleDict.Remove(r,out var v)) {
                _outlineCircleDict.Add(r, new Circle(v.CartesianPosition));
            }
        });

        toRemove = _outlineCircleDict.AsParallel().Where(e => {
            var triangularCoordPos = e.Key;
            var circle = e.Value;
            if(circle.ReadyToDim) {
                if(circle.DimmingProgress >= dimTime) {
                    return true;
                }
                Vector2 cartesianPos = circle.CartesianPosition;
                circle.DimmingProgress = Math.Min(dimTime, circle.DimmingProgress + skippedTime);
                float percent = circle.DimmingProgress / dimTime;
                int stage = (int)(percent * 10 + 0.1);
                if(stage > circle.Milestone) {
                    circle.Milestone = stage;
                    toDim.TryAdd(triangularCoordPos, new (cartesianPos, stage / 10f));
                }
            } else {
                circle.Timer += skippedTime;
                if(circle.Timer > stayVisibleTime) circle.ReadyToDim = true;
            }
            return false;
        }).Select(e => e.Key).ToList();

        _outlineCircleDict.RemoveAll(toRemove);

        // get polygon to cut
        Parallel.ForEach(toDim, e => FindConnectingNeighbors(e.Key, e.Value, toDim));
        return toDim;
    }

    private List<Polygon> MergeDimPolygons(ConcurrentDictionary<Vector2Int,DimProfile> toDim) {
        if(toDim.IsEmpty()) return new();
        HashSet<Vector2Int> visited = new();
        List<Polygon> nonIntersectingPolygons = new();
        Polygon currentPolygon = null;
        Queue<Vector2Int[]> next = new();
        next.Enqueue(new Vector2Int[] {toDim.First().Key});
        while(toDim.NonEmpty()) {
            Vector2Int[] triPos;
            if(!next.TryDequeue(out triPos)) {
                nonIntersectingPolygons.Add(currentPolygon);
                currentPolygon = null;
                next.Enqueue(new Vector2Int[] {toDim.First().Key});
                continue;
            }
            List<Vector2Int> toMerge = new();
            List<List<Vector2Int>> nextNonIntersectNeighbors = new();
            Dictionary<Vector2Int,DimProfile> used = new();
            triPos.ForEach(pos => {
                if(toDim.Remove(pos, out DimProfile profile)) {
                    int count = 0;
                    if(!profile.ConnectingNeighborDims.IsEmpty()) {
                        for(int i = 0; i<12; i+=2) {
                            Vector2Int k;
                            List<Vector2Int> tempNonintersect = new();
                            if(profile.ConnectingNeighborDims.TryGetValue(i, out k)) {
                                tempNonintersect.Add(k);
                                count+=1;
                            }
                            if(profile.ConnectingNeighborDims.TryGetValue(i+1, out k)) {
                                tempNonintersect.Add(k);
                                count+=1;
                            }
                            if(tempNonintersect.NonEmpty()) nextNonIntersectNeighbors.Add(tempNonintersect);
                            if(count>=profile.ConnectingNeighborDims.Count) break;
                        }
                    }
                    used.Add(pos, profile);
                    toMerge.Add(pos);
                }
            });
            nextNonIntersectNeighbors.ForEach(n => next.Enqueue(n.ToArray()));

            if(currentPolygon == null) currentPolygon = CreateCirclePolygon(used[toMerge[0]].CartesianPosition, used[toMerge[0]].Percent);
            else {
                List<Polygon> toMergePolygons = toMerge.Select(r=>used[r])
                        .Select(prf=>CreateCirclePolygon(prf.CartesianPosition, prf.Percent)).ToList();
                var res = Merge(toMergePolygons, currentPolygon, false);
                if(res.Count>1) {
                    res.RemoveWhere(r => {
                        int index = toMergePolygons.IndexOf(r);
                        if(index != -1) {
                            var v = toMerge[index];
                            toDim.TryAdd(v, used[v]);
                            return true;
                        }
                        return false;
                    });
                }
                currentPolygon = res[0];
            }
        }
        if(currentPolygon!=null) nonIntersectingPolygons.Add(currentPolygon);
        return nonIntersectingPolygons;
    }

    private void FindConnectingNeighbors(Vector2Int center, DimProfile profile, ConcurrentDictionary<Vector2Int,DimProfile> candidates) {
        int count = 0;
        profile.ConnectingNeighborDims = new();
        foreach(var n in GetTriagularCoordNeighbors(center)) {
            if(candidates.TryGetValue(n, out var neighbor) && IsCircleIntersect(neighbor.Percent, profile.Percent, count<=1)) {
                profile.ConnectingNeighborDims.Add(count, n);
            }
            count++;
        }

        foreach(var n in GetOuterTriagularCoordNeighbors(center)) {
            if(candidates.TryGetValue(n, out var neighbor) && IsOuterCircleIntersect(neighbor.Percent, profile.Percent)) {
                profile.ConnectingNeighborDims.Add(count, n);
            }
            count++;
        }
    }

    private bool IsOuterCircleIntersect(float percentA, float percentB) {
        return percentA + percentB >=1.732;
    }

    private bool IsCircleIntersect(float percentA, float percentB, bool isXAxis) {
        if(percentA + percentB < 1) return false;
        if(isXAxis) return true;
        if(percentA < 0.3) return 0.93185 * percentA + percentB >= 1;
        if(percentB < 0.3) return percentA + 0.93185 * percentB >= 1;
        return true;
    }

    private Dictionary<Vector2Int,Vector2> RefreshOutlineCircles(HashSet<Vector2Int> circlesInCurrentVision) {
        List<Vector2Int> outliners = new();
        Dictionary<Vector2Int,Vector2> outlinersNearCurrentVision = new();
        HashSet<Vector2Int> outlinerNearNonCurrentVision = new();
        HashSet<Vector2Int> toReset = new();

        _dimCircleDict
            .ForEach(e=> {
                var c = e.Key;
                var circle = e.Value;

                bool isInCurrentVision = circlesInCurrentVision.Contains(c);

                GetTriagularCoordNeighbors(c).Where(neighbor => !_dimCircleDict.ContainsKey(neighbor))
                    .Peek(outline => outliners.Add(outline))
                    .ForEach(outline => {
                        if(_outlineCircleDict.AddOrCompute(outline, new Circle(Round(ConvertTriangularToCartesianCoord(outline), 2)),
                            d=>{
                                    if(isInCurrentVision) {
                                        toReset.Add(outline);
                                        outlinersNearCurrentVision.AddIfAbsent(outline, d.CartesianPosition);
                                    } else {
                                        outlinerNearNonCurrentVision.Add(outline);
                                    }
                                }))
                        {
                            if(isInCurrentVision) {
                                outlinersNearCurrentVision.Add(outline,
                                            _outlineCircleDict[outline].CartesianPosition);
                            } else {
                                outlinerNearNonCurrentVision.Add(outline);
                            }
                        }
                    });
                if (isInCurrentVision) {
                    circle.Reset();
                }
            });
        _outlineCircleDict.RetainAll(outliners, out Dictionary<Vector2Int,Circle> removedCircles);
        removedCircles.ForEach((k,v) => outlinersNearCurrentVision.AddIfAbsent(k,v.CartesianPosition));
        outlinersNearCurrentVision.RemoveAll(outlinerNearNonCurrentVision);
        toReset.Except(outlinerNearNonCurrentVision).Select(p=>_outlineCircleDict[p]).ForEach(c=>c.Reset());

        List<List<Vector2Int>> outlinePolygonVertices = new();

        return outlinersNearCurrentVision;
    }

    Dictionary<float, List<Vector2>> _circleOffsets;
    private Polygon CreateCirclePolygon(Vector2 center, float progress) { // 0.1,0.2,...
        List<Vector2> offsets = new();
        if(_circleOffsets.TryGetValue(progress, out var os)) {
            offsets = os;
        } else {
            Vector2 vec = new Vector2(0,_circleRadius * progress);
            var step = progress switch
            {
                < 0.3f => 45,
                < 0.6f => 30,
                _ => 15,
            };
            for (int i = 0; i<=180; i+= step) {
                Vector2 temp= Rotate(vec, i * Mathf.Deg2Rad);
                offsets.Add(Round(temp, 2));
                if(i!=0 && i!= 180) {
                    temp = Rotate(vec, - i * Mathf.Deg2Rad);
                    offsets.Insert(0,Round(temp,2));
                }
            }
            _circleOffsets.Add(progress, offsets);
        }

        return new Polygon(offsets.Select(o=>center + o).ToList(), false);
    }

    private Vector2Int NearestCirclePosition(Vector2Int point) {
        Vector2 p = ConvertCartesianToTriangularCoord(point);
        return Round(p);

    }

    private bool AnyNeighborCircleDimmed(Vector2Int triangularCoordPoint) {
        return GetTriagularCoordNeighbors(triangularCoordPoint)
            .Any(n => {
                Circle c;
                return (_dimCircleDict.TryGetValue(n, out c) || _outlineCircleDict.TryGetValue(n, out c)) && c.DimmingProgress >= dimTime;
            });
    }

    private IEnumerable<Vector2Int> GetTriagularCoordNeighbors(Vector2Int center) {
        return new Vector2Int[] {new(center.x - 1, center.y),
                                 new(center.x + 1, center.y),
                                 new(center.x, center.y - 1),
                                 new(center.x, center.y + 1),
                                 new(center.x + 1, center.y + 1),
                                 new(center.x - 1, center.y - 1)};
    }

    private IEnumerable<Vector2Int> GetOuterTriagularCoordNeighbors(Vector2Int center) {
        return new Vector2Int[] {new(center.x - 1, center.y + 1),
                                 new(center.x + 1, center.y - 1),
                                 new(center.x - 2, center.y - 1),
                                 new(center.x + 2, center.y + 1),
                                 new(center.x - 1, center.y - 2),
                                 new(center.x + 1, center.y + 2)};
    }

    private Vector2 ConvertCartesianToTriangularCoord(Vector2Int point) {
        float y = point.y * 2 / (float)Math.Sqrt(3) / _circleRadius;
        float x = (point.y / (float)Math.Sqrt(3) + point.x) / _circleRadius;
        return new (x, y);
    }

    private Vector2 ConvertTriangularToCartesianCoord(Vector2Int point) {
        float y = point.y * _circleRadius * 0.8660254f;
        float x = (point.x - point.y / 2f) * _circleRadius;
        return new (x, y);
    }
#endregion
}
