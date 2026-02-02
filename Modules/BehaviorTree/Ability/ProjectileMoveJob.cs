using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

[BurstCompile]
struct ProjectileMoveJob : IJobParallelForTransform
{
    public float deltaTime;
    [ReadOnly] public NativeArray<ProjectileStatus> statuses;
    [WriteOnly] public NativeArray<ProjectileMoveResult> results;

    public void Execute(int index, TransformAccess transform)
    {
        ProjectileStatus status = statuses[index];
        Vector3 groundDirection = status.IsTrackingTarget()
            ? Projectile.TrackTarget(status.target, status.me, status.groundDirection,
                status.trackTargetMaxRadiansPerSecond, deltaTime)
            : status.groundDirection;
        Vector3 deltaMovement = Projectile.ChangeDirectionAndSpeed(status.groundSpeed, status.endGroundSpeed,
            status.acceleration,
            groundDirection, status.ySpeed, status.gravity, deltaTime, out float newGroundSpeed, out float newYSpeed);

        results[index] = new ProjectileMoveResult
        {
            groundSpeed = newGroundSpeed,
            ySpeed = newYSpeed,
            groundDirection = groundDirection
        };

        transform.position += deltaMovement;
        if (status.faceMoveDirection && deltaMovement != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(deltaMovement, Vector3.up);
    }
}

public struct ProjectileStatus
{
    public Vector3 me;
    public Vector3 target;
    public float trackTargetMaxRadiansPerSecond;
    public Vector3 groundDirection;
    public float groundSpeed;
    public float endGroundSpeed;
    public float acceleration;
    public float ySpeed;
    public float gravity;
    public bool faceMoveDirection;
        
    public bool IsTrackingTarget() => trackTargetMaxRadiansPerSecond > 0;
}

public struct ProjectileMoveResult
{
    public float groundSpeed;
    public float ySpeed;
    public Vector3 groundDirection;
}