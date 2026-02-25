using System;
using System.Collections.Generic;
using EditorAttributes;
using KBCore.Refs;
using Sisus.ComponentNames;
using UnityEngine;

namespace ModularFramework.Modules.Ability
{
    /// <summary>
    /// Handles the movement of a projectile in the game ONLY.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(AssetIdentity))]
    public class Projectile : MonoBehaviour
    {
        [Header("Config"), OnValueChanged(nameof(RenameComponent))]
        public bool isPooling;

        [SerializeField, Min(0.01f), Suffix("s")]
        private float lifetime = 1;

        [SerializeField] private bool faceMoveDirection = true;


        [SerializeField] private bool constantGroundSpeed = true;

        [SerializeField, Min(0.01f), Tooltip("Ground speed, not include y axis")]
        private float speed = 1;

        [SerializeField, HideField(nameof(constantGroundSpeed)), Min(0), Tooltip("Ground speed, not include y axis")]
        private float endSpeed;

        [SerializeField, HideField(nameof(constantGroundSpeed)), Suffix("/s"), Min(0)]
        private float changeSpeedTime = 1f;


        [SerializeField, Min(0), Suffix("/s")] private float gravity = 0f;

        public AimType aimType;

        [SerializeField, ShowField(nameof(aimType), AimType.Transform)]
        private bool followTarget;

        [SerializeField, Min(0), Suffix("/s"), ShowField(nameof(followTarget))]
        private float trackTargetMaxAngle = 30;


        public CastType collisionDetection = CastType.RAYCAST;
        [ShowField(nameof(NeedRadius))] public float radius;

        [ShowField(nameof(collisionDetection), CastType.BOXCAST)]
        public Vector3 halfExtents;

        [ShowField(nameof(collisionDetection), CastType.CAPSULECAST)]
        public Vector3 pointA;

        [ShowField(nameof(collisionDetection), CastType.CAPSULECAST)]
        public Vector3 pointB;

        [Self(Flag.Optional), HideInInspector] public ProjectileEffect effect;

        private bool NeedRadius => collisionDetection is CastType.SPHERECAST or CastType.CAPSULECAST;
        
        [SerializeField, HideInInspector] private float groundSpeed;
        [SerializeField, HideInInspector] private float acceleration;
        private float _ySpeed;
        private float _startTime;
        private Transform _target;
        private Vector3 _groundDirection;
        private Vector3 _startPosition;

        private float _trackTargetMaxRadiansPerSecond;

        [HideInInspector] public uint assetId;

        public Vector3 Direction => faceMoveDirection
            ? transform.forward
            : new Vector3(_groundDirection.x, _ySpeed / groundSpeed, _groundDirection.z).normalized;

        public void Initialize(ProjectileInitialState initialState)
        {
            _target = initialState.target;
            _trackTargetMaxRadiansPerSecond = initialState.trackTargetMaxRadiansPerSecond;
            _groundDirection = initialState.groundDirection;
            groundSpeed = initialState.groundSpeed;
            _ySpeed = initialState.ySpeed;
            _startPosition = transform.position;
        }

        public ProjectileInitialState Initialize(Vector3 startPos, Quaternion startRot, float now,
            Transform target, Vector3? targetPos, Vector3? direction)
        {
            _startTime = now;
            transform.position = startPos;
            transform.rotation = startRot;
            _startPosition = startPos;
            switch (aimType)
            {
                case AimType.Direction:
                    InitializeByDirection(direction ??
                                          (target != null
                                              ? target.position - startPos
                                              : targetPos.HasValue
                                                  ? targetPos.Value - startPos
                                                  : Vector3.zero));
                    break;
                case AimType.Transform or AimType.Self when target != null:
                    InitializeByTransform(target);
                    break;
                case AimType.Transform or AimType.Self or AimType.Position:
                    if (aimType is AimType.Transform or AimType.Self)
                    {
                        Debug.LogError($"{nameof(target)} is null but AimType is Transform, fall back to Position");
                    }

                    InitializeByPosition(target != null ? target.position : targetPos ?? Vector3.zero);
                    break;
            }
            return new ProjectileInitialState()
            {
                groundDirection = _groundDirection,
                groundSpeed = groundSpeed,
                ySpeed = _ySpeed,
                target = _target,
                trackTargetMaxRadiansPerSecond = _trackTargetMaxRadiansPerSecond
            };
        }

        private void InitializeByTransform(Transform target)
        {
            _target = followTarget ? target : null;
            _trackTargetMaxRadiansPerSecond = _target == null ? 0 : trackTargetMaxAngle * Mathf.Deg2Rad;
            _groundDirection = new Vector3(target.position.x - transform.position.x, 0,
                target.position.z - transform.position.z).normalized;
            CalculateRoute(target.position);
        }

        private void InitializeByPosition(Vector3 target)
        {
            _target = null;
            _trackTargetMaxRadiansPerSecond = 0;
            _groundDirection = new Vector3(target.x - transform.position.x, 0, target.z - transform.position.z)
                .normalized;
            CalculateRoute(target);
        }

        private void InitializeByDirection(Vector3 direction)
        {
            _target = null;
            _trackTargetMaxRadiansPerSecond = 0;
            var groundVector = new Vector3(direction.x, 0, direction.z);
            _groundDirection = groundVector.normalized;
            _ySpeed = groundSpeed * direction.y / groundVector.magnitude;
            if (faceMoveDirection) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void CalculateRoute(Vector3 targetPos)
        {
            float yDiff = targetPos.y - transform.position.y;
            float estimatedFlightTime;

            if (constantGroundSpeed)
            {
                estimatedFlightTime = Vector3.Distance(targetPos.IgnoreY(),
                    transform.position.IgnoreY()) / groundSpeed;
            }
            else
            {
                float d = Vector3.Distance(new Vector3(targetPos.x, 0, targetPos.z),
                    new Vector3(transform.position.x, 0, transform.position.z));
                float delta = d - 0.5f * (groundSpeed + endSpeed) * changeSpeedTime;
                estimatedFlightTime = delta > 0
                    ? delta / endSpeed + changeSpeedTime
                    : CalculateTime(groundSpeed, acceleration, d);

            }

            _ySpeed = estimatedFlightTime <= 0
                ? 0
                : gravity > 0
                    ? yDiff / estimatedFlightTime + 0.5f * gravity * estimatedFlightTime
                    : yDiff / estimatedFlightTime;
            var direction = new Vector3(_groundDirection.x, _ySpeed / groundSpeed, _groundDirection.z).normalized;

            if (faceMoveDirection) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private float CalculateTime(float startSpeed, float acceleration, float distance)
        {
            // Using: d = v₀t + ½at²
            // Rearranged to: ½at² + v₀t - d = 0
            // Quadratic formula: t = (-b ± √(b² - 4ac)) / 2a

            float a = 0.5f * acceleration;
            float b = startSpeed;
            float c = -distance;

            float discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                Debug.LogWarning("No real solution for the given parameters");
                return -1;
            }

            float t1 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
            float t2 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);

            // Return the positive time value
            return t1 > 0 ? t1 : t2;
        }


        public bool ReachEndOfLife(float now) => lifetime <= now - _startTime;

        public ProjectileStatus Export() => new ProjectileStatus
        {
            me = transform.position,
            target = _target == null ? Vector3.zero : _target.position,
            trackTargetMaxRadiansPerSecond = _trackTargetMaxRadiansPerSecond,
            groundDirection = _groundDirection,
            groundSpeed = groundSpeed,
            endGroundSpeed = endSpeed,
            acceleration = acceleration,
            ySpeed = _ySpeed,
            gravity = gravity,
            faceMoveDirection = faceMoveDirection
        };

        public void Read(ProjectileMoveResult moveResult)
        {
            groundSpeed = moveResult.groundSpeed;
            _ySpeed = moveResult.ySpeed;
            _groundDirection = moveResult.groundDirection;
        }

        public Vector3[] GetTrajectory()
        {
            var endPosition = transform.position;
            if (aimType is AimType.Direction)
            {
                return new[] {_startPosition, endPosition};
            }

            List<Vector3> trajectory = new List<Vector3>();
            trajectory.Add(_startPosition);

            const float sampleTime = 0.5f;
            Vector3 currentPos = _startPosition;
            float currentGroundSpeed = groundSpeed;
            float currentYSpeed = _ySpeed;

            float totalHorizontalDist = Vector3.Distance(
                new Vector3(_startPosition.x, 0, _startPosition.z),
                new Vector3(endPosition.x, 0, endPosition.z));
            float traveledHorizontalDist = 0f;

            while (traveledHorizontalDist < totalHorizontalDist)
            {
                Vector3 displacement = ChangeDirectionAndSpeed(
                    currentGroundSpeed, endSpeed, acceleration,
                    _groundDirection, currentYSpeed, gravity, sampleTime,
                    out float newGroundSpeed, out float newYSpeed);

                float stepHorizontalDist = new Vector3(displacement.x, 0, displacement.z).magnitude;
                if (stepHorizontalDist <= 0) break;

                float remaining = totalHorizontalDist - traveledHorizontalDist;
                if (stepHorizontalDist > remaining)
                {
                    float ratio = remaining / stepHorizontalDist;
                    displacement *= ratio;
                    newGroundSpeed = Mathf.Lerp(currentGroundSpeed, newGroundSpeed, ratio);
                    newYSpeed = Mathf.Lerp(currentYSpeed, newYSpeed, ratio);
                }

                traveledHorizontalDist += stepHorizontalDist;
                currentPos += displacement;
                currentGroundSpeed = newGroundSpeed;
                currentYSpeed = newYSpeed;
                trajectory.Add(currentPos);
            }

            return trajectory.ToArray();
        }
        
        public static Vector3 TrackTarget(Vector3 target, Vector3 me, Vector3 groundDirection,
            float trackTargetMaxRadiansPerSecond, float deltaTime)
        {
            Vector3 targetDirection = target - me;
            targetDirection.y = 0;
            return Vector3.RotateTowards(groundDirection, targetDirection.normalized,
                trackTargetMaxRadiansPerSecond / deltaTime, 2);
        }

        public static Vector3 ChangeDirectionAndSpeed(float groundSpeed, float endGroundSpeed, float acceleration,
            Vector3 groundDirection, float ySpeed, float gravity, float deltaTime,
            out float newGroundSpeed, out float newYSpeed)
        {
            newGroundSpeed = groundSpeed;
            newYSpeed = ySpeed;

            // Accelerate
            if (acceleration > 0)
            {
                if (newGroundSpeed < endGroundSpeed)
                {
                    newGroundSpeed += acceleration * deltaTime;
                    if (newGroundSpeed > endGroundSpeed) newGroundSpeed = endGroundSpeed;
                }
            }
            // Decelerate
            else if (acceleration < 0)
            {
                if (newGroundSpeed > endGroundSpeed)
                {
                    newGroundSpeed += acceleration * deltaTime;
                    if (newGroundSpeed < endGroundSpeed) newGroundSpeed = endGroundSpeed;
                }
            }

            // Gravity effect
            if (gravity > 0)
            {
                newYSpeed -= gravity * deltaTime;
            }

            return groundDirection * 0.5f * (newGroundSpeed + groundSpeed) * deltaTime +
                   Vector3.up * 0.5f * (newYSpeed + ySpeed) * deltaTime;
        }

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
        
        public void CalculateLifetime(float maxRange)
        {
            if (aimType == AimType.Direction)
            {
                lifetime = constantGroundSpeed? maxRange / speed : CalculateTime(speed, acceleration, maxRange);
            }
        }
    
        [Button]
        public void Validate()
        {
            if(collisionDetection != CastType.SPHERECAST && collisionDetection != CastType.CAPSULECAST) radius = -1f;
            if(collisionDetection != CastType.BOXCAST) halfExtents = Vector3.one * -1f;
            constantGroundSpeed = constantGroundSpeed || Mathf.Approximately(speed, endSpeed);
            groundSpeed = speed;
            if (constantGroundSpeed)
            {
                changeSpeedTime = 0;
                endSpeed = groundSpeed;
            }
            acceleration = constantGroundSpeed? 0 : (endSpeed - groundSpeed) / changeSpeedTime;
            if (aimType != AimType.Transform) followTarget = false;
        }
#endif

        private void RenameComponent()
        {
            if (isPooling) this.SetName("Projectile (pooling)");
            else this.SetName("Projectile");
        }

        private void OnDrawGizmosSelected()
        {
            if (collisionDetection == CastType.SPHERECAST && radius > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
            else if (collisionDetection == CastType.BOXCAST && halfExtents.IsPositive())
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, halfExtents * 2);
            }
            else if (collisionDetection == CastType.CAPSULECAST && radius > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + pointA, radius);
                Gizmos.DrawWireSphere(transform.position + pointB, radius);
            }
        }
    }
    
    [Serializable]
    public struct ProjectileInitialState
    {
        // public Vector3 targetPos;
        // public uint targetNetId;
        public Transform target;
        public float trackTargetMaxRadiansPerSecond;
        public Vector3 groundDirection;
        public float groundSpeed;
        public float ySpeed;
    }
    
    public enum AimType
    {
        Direction,
        Transform,
        Position,
        Self
    }
}