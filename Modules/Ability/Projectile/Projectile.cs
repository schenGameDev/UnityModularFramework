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
        private const float SAMPLE_TIME = 0.2f;
        
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
        
        [SerializeField] bool saveTrajectory;
        
        private float _startYSpeed;
        private float _ySpeed;
        private float _startTime;
        private Transform _target;
        private Vector3 _groundDirection;
        private Vector3 _startPosition;

        private float _trackTargetMaxRadiansPerSecond;
        private readonly List<Vector3> _trajectory = new();
        
        [HideInInspector] public uint assetId;

        public Vector3 Direction => faceMoveDirection
            ? transform.forward
            : new Vector3(_groundDirection.x, _ySpeed / groundSpeed, _groundDirection.z).normalized;

        public bool ReachEndOfLife(float now) => lifetime <= now - _startTime;

        #region Initialization
        public void Initialize(ProjectileInitialState initialState)
        {
            _target = initialState.target;
            _trackTargetMaxRadiansPerSecond = initialState.trackTargetMaxRadiansPerSecond;
            _groundDirection = initialState.groundDirection;
            groundSpeed = initialState.groundSpeed;
            _ySpeed = initialState.ySpeed;
            _startPosition = transform.position;
            _trajectory.Clear();
            _trajectory.Add(_startPosition);
            _startYSpeed = _ySpeed;
        }

        public ProjectileInitialState Initialize(Vector3 startPos, Quaternion startRot, float now,
            Transform target, Vector3? targetPos, Vector3? direction)
        {
            _startTime = now;
            transform.position = startPos;
            transform.rotation = startRot;
            _startPosition = startPos;
            _trajectory.Clear();
            _trajectory.Add(startPos);
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
            
            _startYSpeed = _ySpeed;
            
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
        #endregion

        #region Calculation
        private void CalculateRoute(Vector3 targetPos)
        {
            var groundDistance = Vector3.Distance(new Vector3(targetPos.x, 0, targetPos.z),
                new Vector3(transform.position.x, 0, transform.position.z));
            float yDiff = targetPos.y - transform.position.y;
            _ySpeed = constantGroundSpeed
                ? CalculateStartYSpeedConstantSpeed(yDiff, gravity,groundDistance, speed)
                : CalculateStartYSpeed(yDiff, gravity, groundDistance, speed, endSpeed, changeSpeedTime, acceleration);

            var direction = new Vector3(_groundDirection.x, _ySpeed / groundSpeed, _groundDirection.z).normalized;
            if (faceMoveDirection) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        
        public static float CalculateStartYSpeedConstantSpeed(float yDiff, float gravity, float groundDist, float groundSpd)
        {
            float flightTime = groundDist / groundSpd;
            return flightTime <= 0 
                ? 0 
                : gravity > 0 
                    ? yDiff / flightTime + 0.5f * gravity * flightTime 
                    : yDiff / flightTime;
        }

        public static float CalculateStartYSpeed(float yDiff, float gravity, float groundDist, float startGroundSpd, 
            float endGroundSpd, float changeSpdTime, float acceleration)
        {
            float delta = groundDist- 0.5f * (startGroundSpd + endGroundSpd) * changeSpdTime;
            float estimatedFlightTime = delta > 0
                ?  delta / endGroundSpd + changeSpdTime 
                : CalculateTime(startGroundSpd, acceleration, groundDist);
            return estimatedFlightTime <= 0 
                ? 0 
                : gravity > 0 
                    ? yDiff / estimatedFlightTime + 0.5f * gravity * estimatedFlightTime 
                    : yDiff / estimatedFlightTime;
        }

        private static float CalculateTime(float startSpeed, float acceleration, float distance)
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

            return groundDirection * (0.5f * (newGroundSpeed + groundSpeed) * deltaTime) +
                   Vector3.up * (0.5f * (newYSpeed + ySpeed) * deltaTime);
        }
        #endregion
        #region Read & Export
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
        #endregion
        #region Trajectory
        public void SavePosition()
        {
            if (!saveTrajectory) return;
            if (aimType == AimType.Direction && _trajectory.Count > 1)
            {
                _trajectory[1] = transform.position;
            }
            else
            {
                _trajectory.Add(transform.position);
            }
        }
        
        public Vector3[] GetTrajectory()
        {
            if (saveTrajectory && _trajectory.Count > 1) return _trajectory.ToArray();
            var endPosition = transform.position;
            if (aimType is AimType.Direction)
            {
                return new[] {_startPosition, endPosition};
            }
            return CalculateTrajectory(_startPosition, endPosition, speed, endSpeed, acceleration, _groundDirection, _startYSpeed, gravity);
        }
        
        public Vector3[] PredictTrajectory(Vector3 startPos, Vector3 endPos)
        {
            if (aimType is AimType.Direction)
            {
                return new[] {startPos, endPos};
            }
            var groundDirection = new Vector3(endPos.x - startPos.x, 0, endPos.z - startPos.z).normalized;
            var groundDistance = Vector3.Distance(new Vector3(endPos.x, 0, endPos.z), new Vector3(startPos.x, 0, startPos.z));
            var yDiff = endPos.y - startPos.y;
            var startYSpd = constantGroundSpeed
                ? CalculateStartYSpeedConstantSpeed(yDiff, gravity,groundDistance, speed)
                : CalculateStartYSpeed(yDiff, gravity, groundDistance, speed, endSpeed, changeSpeedTime, acceleration);
            return CalculateTrajectory(startPos, endPos, speed, endSpeed, acceleration, groundDirection, startYSpd, gravity);
        }
        
        public static Vector3[] CalculateTrajectory(Vector3 startPos, Vector3 endPos, float startGroundSpeed, float endGroundSpeed,
            float acceleration, Vector3 groundDirection, float startYSpeed, float gravity)
        {
            List<Vector3> trajectory = new ();
            trajectory.Add(startPos);
        
            Vector3 currentPos = startPos;
            float currentGroundSpeed = startGroundSpeed;
            float currentYSpeed = startYSpeed;

            float totalHorizontalDist = Vector3.Distance(
                new Vector3(startPos.x, 0, startPos.z),
                new Vector3(endPos.x, 0, endPos.z));
            float traveledHorizontalDist = 0f;

            while (traveledHorizontalDist < totalHorizontalDist)
            {
                Vector3 displacement = ChangeDirectionAndSpeed(
                    currentGroundSpeed, endGroundSpeed, acceleration,
                    groundDirection, currentYSpeed, gravity, SAMPLE_TIME,
                    out float newGroundSpeed, out float newYSpeed);

                float stepHorizontalDist = new Vector3(displacement.x, 0, displacement.z).magnitude;
                if (stepHorizontalDist <= 0) break;

                float remaining = totalHorizontalDist - traveledHorizontalDist;
                if (stepHorizontalDist > remaining)
                {
                    // float ratio = remaining / stepHorizontalDist;
                    // displacement *= ratio;
                    // newGroundSpeed = Mathf.Lerp(currentGroundSpeed, newGroundSpeed, ratio);
                    // newYSpeed = Mathf.Lerp(currentYSpeed, newYSpeed, ratio);
                    trajectory.Add(endPos);
                    break;
                }

                traveledHorizontalDist += stepHorizontalDist;
                currentPos += displacement;
                currentGroundSpeed = newGroundSpeed;
                currentYSpeed = newYSpeed;
                trajectory.Add(currentPos);
            }

            return trajectory.ToArray();
        }
        #endregion

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