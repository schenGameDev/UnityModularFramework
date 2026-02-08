using System;
using System.Collections.Generic;
using ModularFramework;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Pool;

public class ProjectileManagerSO : GameModule<ProjectileManagerSO> {
    [SerializeField] private int maxBulletCount = 500;
    [SceneRef("PROJECTILE_PARENT")] 
    private Transform _projectileParent;
    [SceneRef("EFFECT_PARENT")] 
    public Transform effectParent;
    public LayerMask collisionMask;
    
    [RuntimeObject] private readonly Dictionary<uint,Projectile> _nonPoolingProjectiles = new ();
    
    [RuntimeObject] private readonly List<Projectile> _activeProjectiles = new ();
    [RuntimeObject] private readonly List<Projectile> _projectilesToReturn = new ();
    [RuntimeObject] private int _spherecastCount = 0, _boxcastCount = 0, _capsulecastCount = 0;
    
    private TransformAccessArray _projTransforms;
    private NativeArray<ProjectileMoveResult> _moveResults;
    private NativeArray<ProjectileStatus> _projectileStatuses;
    
    [RuntimeObject(noInitialize:true)]private RaycastBatchProcessor _raycastBatchProcessor;

    protected override void OnAwake()
    {
        SingletonRegistry<ProjectileManagerSO>.TryRegister(this);
    }
    protected override void OnStart()
    {
        PrefabPool<Projectile>.Clear();
        _raycastBatchProcessor = new(10000);
    }

    protected override void OnUpdate()
    {
        if(_activeProjectiles.Count == 0) return;
        int subSteps = 5;
        float subStepTime = DeltaTime / subSteps;
        
        // Consider caching the TransformAccessArray if possible
        using (_projTransforms = new TransformAccessArray(_activeProjectiles.Count))
        using (_moveResults = new NativeArray<ProjectileMoveResult>(_activeProjectiles.Count, Allocator.TempJob))
        using (_projectileStatuses = new NativeArray<ProjectileStatus>(_activeProjectiles.Count, Allocator.TempJob)) 
        {
            float now = Time.time;
            int j = 0;
            for (int i = _activeProjectiles.Count; i-- > 0;) {
                Projectile projectile = _activeProjectiles[i];
                if (projectile.ReachEndOfLife(now)) {
                    ReturnProjectile(projectile);
                    continue;
                }
                _projTransforms.Add(projectile.transform);
                _projectileStatuses[j++] = projectile.Export();
            }

            for (int step = 0; step < subSteps; step++) {
                if (step > 0)
                {
                    j = 0;
                    for (int i = _activeProjectiles.Count; i-- > 0;)
                    {
                        _projectileStatuses[j++] = _activeProjectiles[i].Export();
                    }
                }
                var job = new ProjectileMoveJob {
                    deltaTime = subStepTime,
                    results = _moveResults,
                    statuses = _projectileStatuses
                };

                JobHandle jobHandle = job.Schedule(_projTransforms);
                jobHandle.Complete();
                j = 0;
                for (int i = _activeProjectiles.Count; i-- > 0;)
                {
                    _activeProjectiles[i].Read(_moveResults[j++]);
                }
                HandleCollisions(); 
            }
        }
    }
    
    protected override void OnLateUpdate() {
        if(_projectilesToReturn.Count == 0) return;
        foreach (var projectile in _projectilesToReturn) {
            if(!PrefabPool<Projectile>.Release(projectile, projectile.assetId)) {
                DestroyProjectile(projectile);
            }
        }
        _projectilesToReturn.Clear();
    }

    protected override void OnSceneDestroy()
    {
        PrefabPool<Projectile>.Clear();
        if(_projTransforms.isCreated) 
            try
            {
                _projTransforms.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
        if(_moveResults.IsCreated) 
            try
            {
                _moveResults.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
        if(_projectileStatuses.IsCreated) 
            try
            {
                _projectileStatuses.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
    }

    protected override void OnDraw()
    {
    }

    private ObjectPool<Projectile> CreateProjectilePool(uint assetId, Projectile prefab)
    {
        return new ObjectPool<Projectile>(
            createFunc: () => {
                var projectile = Instantiate(prefab, _projectileParent);
                projectile.assetId = assetId;
                projectile.gameObject.SetActive(false);
                return projectile;
            },
            actionOnGet: projectile => projectile.gameObject.SetActive(true),
            actionOnRelease: projectile => projectile.gameObject.SetActive(false),
            actionOnDestroy: DestroyProjectile,
            collectionCheck: false,
            defaultCapacity: 50,
            maxSize: maxBulletCount
        );
    }
    
    private void DestroyProjectile(Projectile projectile) {
        if (projectile) {
            // NetworkServer.
            Destroy(projectile.gameObject);
        }
    }

    private void HandleCollisions() {
        bool[] hitResults = new bool[_activeProjectiles.Count];
        
        Vector3[] rayOrigins = new Vector3[_activeProjectiles.Count - _spherecastCount];
        Vector3[] rayDirections = new Vector3[_activeProjectiles.Count - _spherecastCount];
        int[] rayIndices = new int[_activeProjectiles.Count - _spherecastCount];
        int r = 0;
        
        Vector3[] sphereOrigins = new Vector3[_spherecastCount];
        Vector3[] sphereDirections = new Vector3[_spherecastCount];
        float[] sphereRadii = new float[_spherecastCount];
        int[] sphereIndices = new int[_spherecastCount];
        int s = 0;
        
        Vector3[] boxOrigins = new Vector3[_boxcastCount];
        Vector3[] boxDirections = new Vector3[_boxcastCount];
        Vector3[] boxHalfExtents = new Vector3[_boxcastCount];
        Quaternion[] boxRotations = new Quaternion[_boxcastCount];
        int[] boxIndices = new int[_boxcastCount];
        int b = 0;
        
        Vector3[] capsulePointA = new Vector3[_capsulecastCount];
        Vector3[] capsulePointB = new Vector3[_capsulecastCount];
        Vector3[] capsuleDirections = new Vector3[_capsulecastCount];
        float[] capsuleRadii = new float[_capsulecastCount];
        int[] capsuleIndices = new int[_capsulecastCount];
        int c = 0;
        
        for (int i = 0; i < _activeProjectiles.Count; i++) {
            Projectile projectile = _activeProjectiles[i];
            
            if (projectile.collisionDetection == CastType.SPHERECAST)
            {
                sphereOrigins[s] = projectile.transform.position;
                sphereDirections[s] = projectile.Direction;
                sphereRadii[s] = projectile.radius;
                sphereIndices[s] = i;
                s++;
            }
            else if (projectile.collisionDetection == CastType.BOXCAST)
            {
                boxOrigins[b] = projectile.transform.position;
                boxDirections[b] = projectile.Direction;
                boxHalfExtents[b] = projectile.halfExtents;
                boxRotations[b] = projectile.transform.rotation;
                boxIndices[b] = i;
                b++;
            }
            else if (projectile.collisionDetection == CastType.CAPSULECAST)
            {
                capsulePointA[c] = projectile.pointA;
                capsulePointB[c] = projectile.pointB;
                capsuleDirections[c] = projectile.Direction;
                capsuleRadii[c] = projectile.radius;
                capsuleIndices[c] = i;
                c++;
            }
            else
            {
                rayOrigins[r] = projectile.transform.position;
                rayDirections[r] = projectile.Direction;
                rayIndices[r] = i;
                r++;
            }
            
        }

        if (rayOrigins.Length > 0)
        {
            _raycastBatchProcessor.PerformRaycasts(rayOrigins, rayDirections, collisionMask.value, false, 
                    false, false, hits => OnRaycastResults(hits, rayIndices, ref hitResults));
        }
            
        if(sphereOrigins.Length > 0)
        {
            _raycastBatchProcessor.PerformSpherecasts(sphereOrigins, sphereDirections, sphereRadii, collisionMask.value, false, 
                    false, false,hits => OnRaycastResults(hits, sphereIndices, ref hitResults));
        }
        
        if (boxOrigins.Length > 0)
        {
            _raycastBatchProcessor.PerformBoxcasts(boxOrigins, boxDirections, boxHalfExtents, boxRotations, collisionMask.value, 
                false,false, false,hits => OnRaycastResults(hits, boxIndices, ref hitResults));
        }
        
        if (capsulePointA.Length > 0)
        {
            _raycastBatchProcessor.PerformCapsulecasts(capsulePointA, capsulePointB, capsuleDirections, capsuleRadii, collisionMask.value, 
                false,false, false,hits => OnRaycastResults(hits, capsuleIndices, ref hitResults));
        }
        
        for (int i = hitResults.Length; i-- > 0;) {
            if (hitResults[i]) {
                ReturnProjectile(_activeProjectiles[i]);
            }
        }
    }

    private void OnRaycastResults(RaycastHit[] hits, int[] indexMapping, ref bool[] hitResults) {
        for (int i = hits.Length; i-- > 0;) {
            if (hits[i].collider != null)
            {
                int index = indexMapping[i];
                hitResults[index] = true;
                _activeProjectiles[index].GetComponent<ProjectileEffect>().Arrive(hits[i].transform, hits[i].point);
               
            }
        }
    }
    public Projectile SpawnProjectile(uint assetId, Vector3 startPos, Quaternion startRot,
        Transform target, Vector3? targetPos, Vector3? direction)
    {
        Projectile projectile = CreateProjectile(assetId);
        projectile.Initialize(startPos, startRot, Time.time,target, targetPos, direction);
        _activeProjectiles.Add(projectile);
        if (projectile.collisionDetection == CastType.SPHERECAST) _spherecastCount++;
        else if (projectile.collisionDetection == CastType.BOXCAST) _boxcastCount++;
        else if (projectile.collisionDetection == CastType.CAPSULECAST) _capsulecastCount++;
        return projectile;
    }
    
    
    private Projectile CreateProjectile(uint assetId)
    {
        if (!PrefabPool<Projectile>.TryGet(assetId, out var projectile))
        {
            if (_nonPoolingProjectiles.TryGetValue(assetId, out var prefab))
            {
                projectile = Instantiate(prefab, _projectileParent);
                projectile.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError($"Projectile {assetId} not registered");
            }
        }
        
        return projectile;
    }

    public uint RegisterProjectile(Projectile prefab)
    {
        if (prefab.isPooling)
        {
            return PrefabPool<Projectile>.Register(prefab, CreateProjectilePool);
        }

        uint assetId = prefab.GetComponent<AssetIdentity>().assetId;
        _nonPoolingProjectiles.TryAdd(assetId, prefab);
        return assetId;
    }
    
    private void ReturnProjectile(Projectile projectile) {
        _projectilesToReturn.Add( projectile);
        _activeProjectiles.Remove( projectile);
        if (projectile.collisionDetection == CastType.SPHERECAST) _spherecastCount--;
        else if (projectile.collisionDetection == CastType.BOXCAST) _boxcastCount--;
        else if (projectile.collisionDetection == CastType.CAPSULECAST) _capsulecastCount--;
    }
}