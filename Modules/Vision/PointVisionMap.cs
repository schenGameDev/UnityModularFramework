
using System.Collections.Generic;
using ModularFramework;
using static ModularFramework.Utility.MathUtil;
using static ModularFramework.Utility.GeometryUtil;
using static ModularFramework.Utility.LayerMaskUtil;
using UnityEngine;
using UnityEngine.Jobs;
using System.Linq;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;



[CreateAssetMenu(fileName ="VisionMap_SO",menuName ="Game Module/Point Vision Map")]
public class PointVisionMap : VisionMap
{

    public struct VisionPoint {
        public Vector2Int position;
        public float afterglowTime;
        public float dimmingTime;
        public bool isRemove;
    }

    [Header("Curtain")]

    [SerializeField] private Transform _maskPrefab;

    protected override void Prepare()
    {
        Scan();
        CreateCurtain();
    }

    protected override void CalculateVision()
    {
        UpdateActivePool(skippedTime);
    }

    protected override void ImplementVision()
    {
        UpdatePointCurtain();
    }


    #region Scan
    /// <summary>
    /// Scan scene around player to generate a height map
    /// </summary>
    public void Scan() {
        var halfX = Round(dimension.x/2/ stepSize);
        var halfY = Round(dimension.y/2/ stepSize);
        int lX = halfX*2+1, lY = halfY*2+1;

        int layerMask = CombineLayerToMask(_layers);
        RaycastHit[] hits;

        _totalMaskRegion = new[] {new Vector2Int(-halfX,-halfY), new Vector2Int(halfX,halfY)};
        stepSize = RoundTo(stepSize, 2);

        NativeArray<RaycastCommand> rayCommands;
        NativeArray<Vector2Int> virtualPoints;
        int pointsFound;
        using (rayCommands = new NativeArray<RaycastCommand>(lX * lY, Allocator.TempJob))
        using (virtualPoints = new NativeArray<Vector2Int>(lX * lY, Allocator.TempJob)) {
            QueryParameters parameters = new QueryParameters {
                layerMask = layerMask,
                hitBackfaces = true,
                hitTriggers = QueryTriggerInteraction.Collide,
                hitMultipleFaces = false
            };
            int q = 0;
            for(int i = 0; i<=halfX; i++) {
                for(int j = 0; j<=halfY; j++) {
                    virtualPoints[q++] = new(i,j);
                    if(i!=0) virtualPoints[q++] = new(-i,j);
                    if(j!=0) virtualPoints[q++] = new(i,-j);
                    if(i!=0 && j!=0) virtualPoints[q++] = new(-i,-j);
                }
            }

            var job = new CreateRaycastCommandJob() {
                height = mapHeight,
                stepSize = stepSize,
                halfX = halfX,
                halfY = halfY,
                lY = lY,
                center = center,
                parameters = parameters,
                virtualPoints = virtualPoints,
                raycastCmds = rayCommands
            };

            JobHandle h = job.Schedule(virtualPoints.Length, 64);
            h.Complete();
            pointsFound = ExecuteRaycasts(rayCommands, out hits);
        }

        if(pointsFound<0) return;
        _heightMap = new(pointsFound, Allocator.Persistent);
        int m=-halfX,n = -halfY;
        foreach(var h in hits) {
            if(h.collider != null) {
                _heightMap[new(m,n)] = h.point.y;
            }
            if(n+1 > halfY) {
                m+=1;
                n= -halfY;
            } else {
                n+=1;
            }
        }

        Debug.Log("Created " + _heightMap.Count + " Mask Points");
    }

    [BurstCompile(FloatPrecision=FloatPrecision.Low)]
    struct CreateRaycastCommandJob : IJobParallelFor
    {
        public float height;
        public float stepSize;
        public int halfX;
        public int halfY;
        public int lY;
        public Vector2 center;
        public QueryParameters parameters;
        [Unity.Collections.ReadOnly] public NativeArray<Vector2Int> virtualPoints;
        [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> raycastCmds;
        public void Execute(int index)
        {
            var point = virtualPoints[index];
            Vector3 worldPoint = new Vector3(point.x * stepSize + center.x, height, point.y * stepSize + center.y);
            raycastCmds[(point.x + halfX) * lY + point.y + halfY] = new RaycastCommand(worldPoint, Vector3.down, parameters, 500);
        }
    }

    private int ExecuteRaycasts(NativeArray<RaycastCommand> raycastCommands, out RaycastHit[] results) {
        int maxHitsPerRaycast = 1;
        int totalHitsNeeded = raycastCommands.Length * maxHitsPerRaycast;

        using (NativeArray<RaycastHit> hitResults = new NativeArray<RaycastHit>(totalHitsNeeded, Allocator.TempJob)) {

            JobHandle raycastJobHandle = RaycastCommand.ScheduleBatch(raycastCommands, hitResults, maxHitsPerRaycast);
            raycastJobHandle.Complete();

            results = hitResults.ToArray();
        }
        return results.Where(r=>r.collider != null).Count();
    }

    #endregion Scan


    #region Visible Region
    [RuntimeObject(false)] private NativeHashMap<Vector2Int, float> _heightMap;
    [RuntimeObject] private Dictionary<Vector2Int,VisionPoint> _activePool = new();
    private Vector2Int[] _totalMaskRegion, _playerVisibleRegion; // -x-z, +x+z

    private HashSet<Vector2Int> CheckVisiblePoints() {
        HashSet<Vector2Int> visiblePoints = new();

        var r = Round(PeripheralVisionRadius / stepSize);
        var R = Round(ConeVisionDistance / stepSize);


        float h = Player.position.y + EyeHeight;

        Vector2Int relativePos = NearestPoint(Player.position);
        List<Vector2Int> blockageList = new();
        List<Vector2Int> candidates = new();
        var f = System.Math.Max(r, R);
        for(int i = relativePos.x -f; i <= relativePos.x +f; i++) {
            for(int j = relativePos.y -f; j<= relativePos.y +f; j++) {
                Vector2Int p = new(i,j);
                if(_heightMap.TryGetValue(p, out float worldHeight)) {
                    if(worldHeight > h ) // blocked
                    {
                        blockageList.Add(p); // consider only boundary
                    } else {
                        candidates.Add(p);
                    }
                }
            }
        }

        NativeArray<Vector2Int> pointArray = new (candidates.ToArray(), Allocator.TempJob);
        NativeArray<Vector2Int> blockArray = new (blockageList.ToArray(), Allocator.TempJob);
        NativeArray<bool> visArray = new(candidates.Count, Allocator.TempJob);

        var job = new CheckVisJob() {
            sqrr = r * r,
            sqrR = R * R,
            playerRelativePos = relativePos,
            viewAngleRelativeToZ = Player.GetComponent<IViewer>().ViewAngle,
            halfConeAngle = ConeAngle / 2,
            pointArray = pointArray,
            visArray = visArray,
            blockArray = blockArray
        };
        JobHandle jobHandle = job.Schedule(pointArray.Length, 64);
        jobHandle.Complete();
        pointArray.Dispose();
        blockArray.Dispose();

        int k =0;
        foreach(var p in candidates) {
            if(visArray[k++]) visiblePoints.Add(p);
        }
        visArray.Dispose();

        return visiblePoints;
    }

    [BurstCompile(FloatPrecision=FloatPrecision.Low)]
    struct CheckVisJob : IJobParallelFor
    {
        public float sqrR;
        public float sqrr;
        public Vector2Int playerRelativePos;
        public float viewAngleRelativeToZ;
        public float halfConeAngle;
        [WriteOnly] public NativeArray<bool> visArray;
        [Unity.Collections.ReadOnly] public NativeArray<Vector2Int> pointArray;
        [Unity.Collections.ReadOnly] public NativeArray<Vector2Int> blockArray;
        public void Execute(int index)
        {
            var p = pointArray[index];
            Vector2Int dir = p - playerRelativePos;
            var sqrDist = dir.sqrMagnitude;
            bool inRange = sqrDist <= sqrr ||
                            (sqrDist <= sqrR &&
                             WithinAngleRange(Vector2.SignedAngle(Vector2.up,dir), viewAngleRelativeToZ - halfConeAngle, viewAngleRelativeToZ + halfConeAngle));
            if(!inRange) return;

            bool block = false;
            foreach(var b in blockArray) {
                if(SameDirectionButLonger(p-playerRelativePos, b-playerRelativePos)) {
                    block = true;
                    break;
                }
            }
            if(block) return;

            visArray[index] = true;
        }

    }

    private void UpdateActivePool(float deltaTime) {
        HashSet<Vector2Int> visiblePoints;
        if(Active)
            visiblePoints = CheckVisiblePoints();
        else
            visiblePoints = new HashSet<Vector2Int>();

        Vector2Int playerRelativePos = InitializeVisibleRegion();
        UpdateExistingPoints(playerRelativePos, visiblePoints, deltaTime);
    }

    private void UpdateExistingPoints(Vector2Int playerRelativePos, HashSet<Vector2Int> visiblePoints, float deltaTime) {
        var pointStructs = _activePool.Values.ToArray();

        var input = new NativeArray<VisionPoint>(pointStructs, Allocator.TempJob);
        NativeHashSet<Vector2Int> visible = new (visiblePoints.Count, Allocator.TempJob);
        var output = new NativeArray<VisionPoint>(pointStructs.Length, Allocator.TempJob);

        foreach(var v in visiblePoints) visible.Add(v);


        var job = new UpdateVisiblePointJob() {
            maxDist = maxRange / stepSize,
            orgin = playerRelativePos,
            maxAfterglowTime = stayVisibleTime,
            maxDimTime = dimTime,
            deltaTime = deltaTime,
            pointArray= input,
            visibleSet = visible,
            pointArrayOut = output
        };
        JobHandle jobHandle = job.Schedule(input.Length, 64);
        jobHandle.Complete();

        foreach(var p in output) {
            if(p.isRemove) {
                _activePool.Remove(p.position);
            } else {
                _activePool[p.position] = p;
                UpdateVisibleRegion(p.position);
            }
        }

        foreach(var p in visiblePoints.Except(_activePool.Keys)) {
            _activePool.Add(p, new VisionPoint() {position=p});
            UpdateVisibleRegion(p);
        }
        input.Dispose();
        output.Dispose();
        visible.Dispose();
    }

    [BurstCompile(FloatPrecision=FloatPrecision.Low)]
    struct UpdateVisiblePointJob : IJobParallelFor
    {
        public float maxDist;
        public Vector2Int orgin;
        public float maxAfterglowTime;
        public float maxDimTime;
        public float deltaTime;

        [WriteOnly] public NativeArray<VisionPoint> pointArrayOut;
        [Unity.Collections.ReadOnly] public NativeArray<VisionPoint> pointArray;
        [Unity.Collections.ReadOnly] public NativeHashSet<Vector2Int> visibleSet;
        public void Execute(int index)
        {
            var p = pointArray[index];
            bool isVisible = visibleSet.Contains(p.position);


            bool isDimming = !isVisible && p.dimmingTime>0;
            float newAfterglowTime = isVisible? 0 : (isDimming? p.afterglowTime : p.afterglowTime + deltaTime);
            bool isAfterglow = newAfterglowTime <= maxAfterglowTime;
            float newDimTime = isAfterglow && !isDimming? 0 : p.dimmingTime + deltaTime;

            bool isPersist = isVisible || ((p.position - orgin).sqrMagnitude < maxDist * maxDist && newDimTime < maxDimTime);
            pointArrayOut[index] = new VisionPoint() {position = p.position,
                                                            afterglowTime=newAfterglowTime,
                                                            dimmingTime=newDimTime,
                                                            isRemove = !isPersist};

        }
    }

    private Vector2Int InitializeVisibleRegion() {
        var center = NearestPoint(Player.position);
        _playerVisibleRegion = new[] {center,center};
        return center;
    }

    private void UpdateVisibleRegion(Vector2Int pointToInclude) {
        if(_playerVisibleRegion[0].x > pointToInclude.x) _playerVisibleRegion[0].x = pointToInclude.x;
        if(_playerVisibleRegion[0].y > pointToInclude.y) _playerVisibleRegion[0].y = pointToInclude.y;
        if(_playerVisibleRegion[1].x < pointToInclude.x) _playerVisibleRegion[1].x = pointToInclude.x;
        if(_playerVisibleRegion[1].y < pointToInclude.y) _playerVisibleRegion[1].y = pointToInclude.y;
    }


#endregion


#region Curtain
    Mesh _curtainMesh;
    List<Vector3> _vertices;
    List<int> _triangles;
    private void CreateCurtain() {
        var curtain = Instantiate(_maskPrefab,
                                     new Vector3(center.x,mapHeight,center.y),
                                     Quaternion.identity,
                                     maskParent);
        curtain.localScale = new Vector3(1,0,1);
        _curtainMesh = new Mesh();
        curtain.GetComponent<MeshFilter>().mesh = _curtainMesh;
        _vertices = new();
        _triangles = new();
    }
    private void UpdatePointCurtain() {
        if(_curtainMesh == null) return;

        // change origin to playerVisibleRegion[0]
        var tMin = _totalMaskRegion[0] - _playerVisibleRegion[0];
        var tMax = _totalMaskRegion[1] - _playerVisibleRegion[0];
        var vMin = Vector2Int.zero;
        var vMax = _playerVisibleRegion[1] - _playerVisibleRegion[0];

        _vertices.Clear();
        _triangles.Clear();

        List<Vector2Int> squareList = new () {tMin, new(vMin.x,tMin.y), new(tMin.x,vMin.y), vMin,
                                              new(vMin.x,tMin.y), new(vMax.x,tMin.y), vMin, new(vMax.x,vMin.y),
                                              new(vMax.x,tMin.y), new(tMax.x,tMin.y), new(vMax.x,vMin.y), new(tMax.x,vMin.y),
                                              new(tMin.x,vMin.y), vMin, new(tMin.x,vMax.y), new(vMin.x,vMax.y),
                                              new(vMax.x,vMin.y), new(tMax.x,vMin.y), vMax, new(tMax.x,vMax.y),
                                              new(tMin.x,vMax.y), new(vMin.x,vMax.y), new(tMin.x,tMax.y), new(vMin.x,tMax.y),
                                              new(vMin.x,vMax.y), vMax, new(vMin.x,tMax.y), new(vMax.x,tMax.y),
                                              vMax, new(tMax.x,vMax.y), new(vMax.x,tMax.y), tMax};
        AddBlockMeshInVisibleRegion(squareList);
        AssignNewVertices(squareList);

        if(isDim)
        {
            AddIsolatedMeshInVisibleRegion();
        }

        _curtainMesh.Clear();
        _curtainMesh.vertices = _vertices.ToArray();
        _curtainMesh.triangles = _triangles.ToArray();

    }

    private void AddIsolatedMeshInVisibleRegion()
    {
        var dimmingPointList = _activePool.Values.Where(p=>p.dimmingTime > 0).ToArray();
        var pointArray = new NativeArray<VisionPoint>(dimmingPointList, Allocator.TempJob);

        var squareVertexWorldArray = new NativeArray<Vector3>(pointArray.Length * 4, Allocator.TempJob);

        var job = new IsolateSquareJob() {
            stepSize=stepSize,
            halfStepSize=stepSize / 2,
            dimTotalTime = dimTime,
            visionPointArray = pointArray,
            squareVertexWorldArray=squareVertexWorldArray
        };
        JobHandle jobHandle = job.Schedule(pointArray.Length, 64);
        jobHandle.Complete();
        pointArray.Dispose();
        for(int j=0; j<squareVertexWorldArray.Length; j+=4) {
            int[] idx = new int[4];
            for(int k=0; k<4; k++) {
                var c = squareVertexWorldArray[k + j];
                int m = _vertices.Count;
                _vertices.Add(c);
                idx[k] = m;
            }
            _triangles.Add(idx[0]);
            _triangles.Add(idx[2]);
            _triangles.Add(idx[3]);
            _triangles.Add(idx[0]);
            _triangles.Add(idx[3]);
            _triangles.Add(idx[1]);
        }
        squareVertexWorldArray.Dispose();
    }

    [BurstCompile(FloatPrecision=FloatPrecision.Low)]
    struct IsolateSquareJob : IJobParallelFor
    {
        public float stepSize;
        public float halfStepSize;
        public float dimTotalTime;
        [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<Vector3> squareVertexWorldArray;
        [Unity.Collections.ReadOnly] public NativeArray<VisionPoint> visionPointArray;
        public void Execute(int index)
        {
            var vp = visionPointArray[index];
            var worldPos = new Vector3(vp.position.x * stepSize, 0, vp.position.y * stepSize);
            var halfSize = halfStepSize * vp.dimmingTime / dimTotalTime;
            int startIdx = index * 4;

            squareVertexWorldArray[startIdx] = worldPos - new Vector3(halfSize,0,halfSize);
            squareVertexWorldArray[startIdx+1] = worldPos + new Vector3(halfSize,0,-halfSize);
            squareVertexWorldArray[startIdx+2] = worldPos + new Vector3(-halfSize,0,halfSize);
            squareVertexWorldArray[startIdx+3] = worldPos + new Vector3(halfSize,0,halfSize);
        }
    }

    private void AddBlockMeshInVisibleRegion(List<Vector2Int> squareList) {
        int xLen = _playerVisibleRegion[1].x - _playerVisibleRegion[0].x + 1,
            yLen = _playerVisibleRegion[1].y - _playerVisibleRegion[0].y + 1;
        bool[,] blockingPoints = new bool[xLen, yLen];

        int column=0;
        for(int i = _playerVisibleRegion[0].x; i<=_playerVisibleRegion[1].x; i++) {
            int row=0;
            for(int j = _playerVisibleRegion[0].y; j<=_playerVisibleRegion[1].y; j++) {
                var p = new Vector2Int(i,j);
                if(!_activePool.ContainsKey(p)) {
                    blockingPoints[column, row] = true;
                }
                row++;
            }
            column++;
        }

        for(int i = 0; i<xLen; i++) {
            for(int j=0; j<yLen; j++) {
                if(!blockingPoints[i,j]) continue;
                Vector2Int p = new (i,j);
                Vector2Int bl=p, br, tl, tr;

                int iEnd = i, jEnd = j;
                bool extXSuccess=true, extYSuccess=true;
                do {
                    if(extXSuccess) {
                        for (int y = j ; y<=jEnd; y++) {
                            blockingPoints[iEnd,y] = false;
                        }
                    }
                    if(extYSuccess) {
                        for (int x = i ; x<=iEnd; x++) {
                            blockingPoints[x,jEnd] = false;
                        }
                    }

                    br=new(iEnd,j);
                    tl=new(i,jEnd);
                    tr=new(iEnd,jEnd);

                    extXSuccess= iEnd + 1 < xLen;
                    extYSuccess= jEnd + 1 < yLen;
                    // extend x direction
                    if(extXSuccess) {
                        for (int y = j ; y<=jEnd; y++) {
                            if(!blockingPoints[iEnd + 1,y]) {
                                extXSuccess = false;
                                break;
                            }
                        }
                        if(extXSuccess) {
                            iEnd++;
                        }
                    }
                    // extend y direction
                    if(extYSuccess) {
                        for (int x = i ; x<=iEnd; x++) {
                            if(!blockingPoints[x, jEnd + 1]) {
                                extYSuccess = false;
                                break;
                            }
                        }
                        if(extYSuccess) {
                            jEnd++;
                        }
                    }
                }  while(extXSuccess || extYSuccess);
                squareList.Add(bl);
                squareList.Add(br);
                squareList.Add(tl);
                squareList.Add(tr);
            }
        }
    }

    private void AssignNewVertices(List<Vector2Int> squareList) {
        var squareVertexArray = new NativeArray<Vector2Int>(squareList.ToArray(), Allocator.TempJob);
        var squareVertexWorldArray = new NativeArray<Vector3>(squareList.Count, Allocator.TempJob);

        var job = new ConvertConrnerJob() {
            stepSize = stepSize,
            halfStepSize = stepSize / 2,
            origin = _playerVisibleRegion[0],
            squareVertexArray = squareVertexArray,
            squareVertexWorldArray = squareVertexWorldArray
        };
        JobHandle jobHandle = job.Schedule(squareVertexArray.Length, 64);
        jobHandle.Complete();
        squareVertexArray.Dispose();

        NativeHashMap<Vector3, int> vertices = new(squareVertexWorldArray.Length,Allocator.Temp);
        NativeArray<int> idx = new NativeArray<int>(4, Allocator.Temp);
        int k = 0;
        foreach(var c in squareVertexWorldArray) {
            int m;
            if(vertices.TryGetValue(c, out int v)) {
                m = v;
            } else {
                m = _vertices.Count;
                _vertices.Add(c);
                vertices.Add(c, m);
            }
            idx[k] = m;
            if(k==3) {
                _triangles.Add(idx[0]);
                _triangles.Add(idx[2]);
                _triangles.Add(idx[3]);
                _triangles.Add(idx[0]);
                _triangles.Add(idx[3]);
                _triangles.Add(idx[1]);
                k=0;
            } else {
                k+=1;
            }
        }

        squareVertexWorldArray.Dispose();
        vertices.Dispose();
        idx.Dispose();
    }

    [BurstCompile]
    struct ConvertConrnerJob : IJobParallelFor
    {
        public float stepSize;
        public float halfStepSize;
        public Vector2Int origin;
        [Unity.Collections.ReadOnly] public NativeArray<Vector2Int> squareVertexArray;
        [WriteOnly] public NativeArray<Vector3> squareVertexWorldArray;

        public void Execute(int index)
        {
            Vector2Int v = squareVertexArray[index] +  origin;
            int cornNumber = index % 4;
            Vector3 res;
            switch (cornNumber) {
                case 0: //bl
                    res = new Vector3(v.x * stepSize - halfStepSize, 0, v.y * stepSize - halfStepSize);
                    break;
                case 1://br
                    res = new Vector3(v.x * stepSize + halfStepSize, 0, v.y * stepSize - halfStepSize);
                    break;
                case 2://tl
                    res = new Vector3(v.x * stepSize - halfStepSize, 0, v.y * stepSize + halfStepSize);
                    break;
                default:
                    res = new Vector3(v.x * stepSize + halfStepSize, 0, v.y * stepSize + halfStepSize);
                    break;
            }

            squareVertexWorldArray[index] = res;
        }
    }
    #endregion
}
