using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class RaycastBatchProcessor : IDisposable {
    private readonly int _maxRaycastsPerJob;
    private const float MAX_DISTANCE = 0.05f;

    private NativeArray<RaycastCommand> _rayCommands;
    private NativeArray<RaycastHit> _hitResults;
    private NativeArray<SpherecastCommand> _sphereCommands;
    private NativeArray<BoxcastCommand> _boxCommands;
    private NativeArray<CapsulecastCommand> _capsuleCommands;

    public RaycastBatchProcessor(int maxRaycastsPerJob = 10000)
    {
        _maxRaycastsPerJob = maxRaycastsPerJob;
    }
    
    public void PerformRaycasts(
        Vector3[] origins,
        Vector3[] directions,
        int layerMask,
        bool hitBackfaces,
        bool hitTriggers,
        bool hitMultiFace,
        Action<RaycastHit[]> callback) {
        
        int rayCount = Mathf.Min(origins.Length, _maxRaycastsPerJob);

        var queryTriggerInteraction = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        
        using (_rayCommands = new NativeArray<RaycastCommand>(rayCount, Allocator.TempJob)) {
            QueryParameters parameters = new QueryParameters {
                layerMask = layerMask,
                hitBackfaces = hitBackfaces,
                hitTriggers = queryTriggerInteraction,
                hitMultipleFaces = hitMultiFace
            };

            for (int i = 0; i < rayCount; i++) {
                _rayCommands[i] = new RaycastCommand(origins[i], directions[i], parameters, MAX_DISTANCE);
            }

            ExecuteCasts(_rayCommands, callback);
        }
    }
    
    public void PerformSpherecasts(
        Vector3[] origins,
        Vector3[] directions,
        float[] radii,
        int layerMask,
        bool hitBackfaces,
        bool hitTriggers,
        bool hitMultiFace,
        Action<RaycastHit[]> callback) {
        int rayCount = Mathf.Min(origins.Length, _maxRaycastsPerJob);

        var queryTriggerInteraction = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        
        using (_sphereCommands = new NativeArray<SpherecastCommand>(rayCount, Allocator.TempJob)) {
            QueryParameters parameters = new QueryParameters {
                layerMask = layerMask,
                hitBackfaces = hitBackfaces,
                hitTriggers = queryTriggerInteraction,
                hitMultipleFaces = hitMultiFace
            };

            for (int i = 0; i < rayCount; i++) {
                _sphereCommands[i] = new SpherecastCommand(origins[i], radii[i], directions[i], parameters, MAX_DISTANCE);
            }

            ExecuteCasts(_sphereCommands, callback);
        }
    }
    
    public void PerformBoxcasts(
        Vector3[] origins,
        Vector3[] directions,
        Vector3[] halfExtents,
        Quaternion[] orientations,
        int layerMask,
        bool hitBackfaces,
        bool hitTriggers,
        bool hitMultiFace,
        Action<RaycastHit[]> callback) {
        int rayCount = Mathf.Min(origins.Length, _maxRaycastsPerJob);

        var queryTriggerInteraction = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        
        using (_boxCommands = new NativeArray<BoxcastCommand>(rayCount, Allocator.TempJob)) {
            QueryParameters parameters = new QueryParameters {
                layerMask = layerMask,
                hitBackfaces = hitBackfaces,
                hitTriggers = queryTriggerInteraction,
                hitMultipleFaces = hitMultiFace
            };

            for (int i = 0; i < rayCount; i++) {
                _boxCommands[i] = new BoxcastCommand(origins[i], halfExtents[i], orientations[i],directions[i], parameters, MAX_DISTANCE);
            }

            ExecuteCasts(_boxCommands, callback);
        }
    }
    
    public void PerformCapsulecasts(
        Vector3[] pointAs,
        Vector3[] pointBs,
        Vector3[] directions,
        float[] radii,
        int layerMask,
        bool hitBackfaces,
        bool hitTriggers,
        bool hitMultiFace,
        Action<RaycastHit[]> callback) {
        int rayCount = Mathf.Min(pointAs.Length, _maxRaycastsPerJob);

        var queryTriggerInteraction = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        
        using (_capsuleCommands = new NativeArray<CapsulecastCommand>(rayCount, Allocator.TempJob)) {
            QueryParameters parameters = new QueryParameters {
                layerMask = layerMask,
                hitBackfaces = hitBackfaces,
                hitTriggers = queryTriggerInteraction,
                hitMultipleFaces = hitMultiFace
            };

            for (int i = 0; i < rayCount; i++) {
                _capsuleCommands[i] = new CapsulecastCommand(pointAs[i], pointBs[i], radii[i], directions[i], parameters, MAX_DISTANCE);
            }

            ExecuteCasts(_capsuleCommands, callback);
        }
    }

    private void ExecuteCasts(NativeArray<RaycastCommand> raycastCommands, Action<RaycastHit[]> callback) {
        int maxHitsPerRaycast = 1;
        int totalHitsNeeded = raycastCommands.Length * maxHitsPerRaycast;
        
        using (_hitResults = new NativeArray<RaycastHit>(totalHitsNeeded, Allocator.TempJob)) {
            // foreach (RaycastCommand t in raycastCommands) {
            //     Debug.DrawLine(t.from, t.from + t.direction * 1f, Color.red, 0.5f);
            // }

            JobHandle raycastJobHandle = RaycastCommand.ScheduleBatch(raycastCommands, _hitResults, maxHitsPerRaycast);
            raycastJobHandle.Complete();

            if (_hitResults.Length > 0) {
                RaycastHit[] results = _hitResults.ToArray();

                // for (int i = 0; i < results.Length; i++) {
                //     if (results[i].collider != null) {
                //         Debug.Log($"Hit: {results[i].collider.name} at {results[i].point}");
                //         Debug.DrawLine(raycastCommands[i].from, results[i].point, Color.green, 1.0f);
                //     }
                // }

                callback?.Invoke(results);
            }
        }
    }
    
    private void ExecuteCasts(NativeArray<SpherecastCommand> spherecastCommands, Action<RaycastHit[]> callback) {
        int maxHitsPerRaycast = 1;
        int totalHitsNeeded = spherecastCommands.Length * maxHitsPerRaycast;
        
        using (_hitResults = new NativeArray<RaycastHit>(totalHitsNeeded, Allocator.TempJob)) {
            JobHandle raycastJobHandle = SpherecastCommand.ScheduleBatch(spherecastCommands, _hitResults, maxHitsPerRaycast);
            raycastJobHandle.Complete();

            if (_hitResults.Length > 0) {
                RaycastHit[] results = _hitResults.ToArray();
                callback?.Invoke(results);
            }
        }
    }
    
    private void ExecuteCasts(NativeArray<BoxcastCommand> boxcastCommands, Action<RaycastHit[]> callback) {
        int maxHitsPerRaycast = 1;
        int totalHitsNeeded = boxcastCommands.Length * maxHitsPerRaycast;
        
        using (_hitResults = new NativeArray<RaycastHit>(totalHitsNeeded, Allocator.TempJob)) {
            JobHandle raycastJobHandle = BoxcastCommand.ScheduleBatch(boxcastCommands, _hitResults, maxHitsPerRaycast);
            raycastJobHandle.Complete();

            if (_hitResults.Length > 0) {
                RaycastHit[] results = _hitResults.ToArray();
                callback?.Invoke(results);
            }
        }
    }
    
    private void ExecuteCasts(NativeArray<CapsulecastCommand> capsulecastCommands, Action<RaycastHit[]> callback) {
        int maxHitsPerRaycast = 1;
        int totalHitsNeeded = capsulecastCommands.Length * maxHitsPerRaycast;
        
        using (_hitResults = new NativeArray<RaycastHit>(totalHitsNeeded, Allocator.TempJob)) {
            JobHandle raycastJobHandle = CapsulecastCommand.ScheduleBatch(capsulecastCommands, _hitResults, maxHitsPerRaycast);
            raycastJobHandle.Complete();

            if (_hitResults.Length > 0) {
                RaycastHit[] results = _hitResults.ToArray();
                callback?.Invoke(results);
            }
        }
    }

    public void Dispose()
    {
        if (_rayCommands.IsCreated)
            try
            {
                _rayCommands.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
        if (_sphereCommands.IsCreated)
            try
            {
                _sphereCommands.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
        if (_boxCommands.IsCreated)
            try
            {
                _boxCommands.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
        if (_capsuleCommands.IsCreated)
            try
            {
                _capsuleCommands.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
        if (_hitResults.IsCreated)
            try
            {
                _hitResults.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
    }
}

public enum CastType
{
    RAYCAST,
    SPHERECAST,
    BOXCAST,
    CAPSULECAST
}