using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class RaycastBatchProcessor : IDisposable {
    private int maxRaycastsPerJob;
    private const float MAX_DISTANCE = 1f;

    NativeArray<RaycastCommand> rayCommands;
    NativeArray<RaycastHit> hitResults;
    NativeArray<SpherecastCommand> sphereCommands;


    public RaycastBatchProcessor(int maxRaycastsPerJob = 10000)
    {
        this.maxRaycastsPerJob = maxRaycastsPerJob;
    }
    
    public void PerformRaycasts(
        Vector3[] origins,
        Vector3[] directions,
        int layerMask,
        bool hitBackfaces,
        bool hitTriggers,
        bool hitMultiFace,
        Action<RaycastHit[]> callback) {
        
        int rayCount = Mathf.Min(origins.Length, maxRaycastsPerJob);

        QueryTriggerInteraction queryTriggerInteraction = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        
        using (rayCommands = new NativeArray<RaycastCommand>(rayCount, Allocator.TempJob)) {
            QueryParameters parameters = new QueryParameters {
                layerMask = layerMask,
                hitBackfaces = hitBackfaces,
                hitTriggers = queryTriggerInteraction,
                hitMultipleFaces = hitMultiFace
            };

            for (int i = 0; i < rayCount; i++) {
                rayCommands[i] = new RaycastCommand(origins[i], directions[i], parameters, MAX_DISTANCE);
            }

            ExecuteRaycasts(rayCommands, callback);
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
        int rayCount = Mathf.Min(origins.Length, maxRaycastsPerJob);

        QueryTriggerInteraction queryTriggerInteraction = hitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
        
        using (sphereCommands = new NativeArray<SpherecastCommand>(rayCount, Allocator.TempJob)) {
            QueryParameters parameters = new QueryParameters {
                layerMask = layerMask,
                hitBackfaces = hitBackfaces,
                hitTriggers = queryTriggerInteraction,
                hitMultipleFaces = hitMultiFace
            };

            for (int i = 0; i < rayCount; i++) {
                sphereCommands[i] = new SpherecastCommand(origins[i], radii[i], directions[i], parameters, MAX_DISTANCE);
            }

            ExecuteRaycasts(sphereCommands, callback);
        }
    }

    private void ExecuteRaycasts(NativeArray<RaycastCommand> raycastCommands, Action<RaycastHit[]> callback) {
        int maxHitsPerRaycast = 1;
        int totalHitsNeeded = raycastCommands.Length * maxHitsPerRaycast;
        
        using (hitResults = new NativeArray<RaycastHit>(totalHitsNeeded, Allocator.TempJob)) {
            // foreach (RaycastCommand t in raycastCommands) {
            //     Debug.DrawLine(t.from, t.from + t.direction * 1f, Color.red, 0.5f);
            // }

            JobHandle raycastJobHandle = RaycastCommand.ScheduleBatch(raycastCommands, hitResults, maxHitsPerRaycast);
            raycastJobHandle.Complete();

            if (hitResults.Length > 0) {
                RaycastHit[] results = hitResults.ToArray();

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
    
    private void ExecuteRaycasts(NativeArray<SpherecastCommand> spherecastCommands, Action<RaycastHit[]> callback) {
        int maxHitsPerRaycast = 1;
        int totalHitsNeeded = spherecastCommands.Length * maxHitsPerRaycast;
        
        using (hitResults = new NativeArray<RaycastHit>(totalHitsNeeded, Allocator.TempJob)) {
            // foreach (RaycastCommand t in raycastCommands) {
            //     Debug.DrawLine(t.from, t.from + t.direction * 1f, Color.red, 0.5f);
            // }

            JobHandle raycastJobHandle = SpherecastCommand.ScheduleBatch(spherecastCommands, hitResults, maxHitsPerRaycast);
            raycastJobHandle.Complete();

            if (hitResults.Length > 0) {
                RaycastHit[] results = hitResults.ToArray();

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

    public void Dispose()
    {
        if (rayCommands.IsCreated)
            try
            {
                rayCommands.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
            
        if (sphereCommands.IsCreated)
            try
            {
                sphereCommands.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
        if (hitResults.IsCreated)
            try
            {
                hitResults.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // already disposed before
            }
    }
}