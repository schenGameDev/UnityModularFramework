using EditorAttributes;
using Sisus.ComponentNames;
using UnityEngine;

/// <summary>
/// Handles the movement of a projectile in the game ONLY.
/// </summary>
[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    [Header("Config"),OnValueChanged(nameof(RenameComponent))] 
    public bool isPooling;
    [ShowField(nameof(isPooling)),OnValueChanged(nameof(RenameComponent))] public string uniqueId;
    
    [SerializeField,Min(0.01f),Suffix("s")] private float lifetime = 1;
    [SerializeField] private bool faceMoveDirection = true;
    

    [SerializeField] 
    private bool constantGroundSpeed = true;
    [SerializeField,Min(0.01f),Tooltip("Ground speed, not include y axis")] 
    private float speed=1;
    [SerializeField,HideField(nameof(constantGroundSpeed)),Min(0),Tooltip("Ground speed, not include y axis")] 
    private float endSpeed;
    [SerializeField,HideField(nameof(constantGroundSpeed)),Suffix("/s"),Min(0)] 
    private float changeSpeedTime = 1f;
    
    
    [SerializeField,Min(0),Suffix("/s")] private float gravity = 0f;
    
    public enum AimType
    {
        Direction,
        Transform,
        Position,
    }
    public AimType aimType;
    [SerializeField, ShowField(nameof(aimType), AimType.Transform)] private bool followTarget;
    [SerializeField,Min(0),Suffix("/s"),ShowField(nameof(followTarget))] 
    private float trackTargetMaxAngle = 30;
    
    
    [SerializeField] private bool spherecast;
    [ShowField(nameof(spherecast))] public float radius;
    
    
    // public Vector3 direction;
    private float _groundSpeed;
    private float _ySpeed;
    private float _startTime;
    private Transform _target;
    private Vector3 _groundDirection;
    private float _acceleration;
    private float _trackTargetMaxRadiansPerSecond;

    private void Awake()
    {
        Validate();
    }

    public void Initialize(Vector3 startPos, Quaternion startRot, float now,
        Transform target, Vector3? targetPos, Vector3? direction) 
    {
        _startTime = now;
        transform.position = startPos;
        transform.rotation = startRot;
        
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
            case AimType.Transform:
                InitializeByTransform(target);
                break;
            case AimType.Position:
                InitializeByPosition(target!=null? target.position : targetPos ?? Vector3.zero);
                break;
        }
        
    }

    private void InitializeByTransform(Transform target)
    {
        if (target == null)
        {
            Debug.LogError($"{nameof(target)} is null but AimType is Transform");
            return;
        }
        _target = followTarget? target : null;
        _trackTargetMaxRadiansPerSecond = _target == null? 0 : trackTargetMaxAngle * Mathf.Deg2Rad;
        _groundDirection = new Vector3(target.position.x - transform.position.x, 0, target.position.z - transform.position.z).normalized;
        CalculateRoute(target.position);
    }
    
    private void InitializeByPosition(Vector3 target)
    {
        _target = null;
        _trackTargetMaxRadiansPerSecond = 0;
        _groundDirection = new Vector3(target.x - transform.position.x, 0, target.z - transform.position.z).normalized;
        CalculateRoute(target);
    }
    
    private void InitializeByDirection(Vector3 direction)
    {
        _target = null;
        _trackTargetMaxRadiansPerSecond = 0;
        _groundDirection = new Vector3(direction.x, 0, direction.z).normalized;
        // this.direction = direction;
        _ySpeed = _groundSpeed * direction.y / _groundDirection.magnitude;
        if(faceMoveDirection) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }
    
    public void Validate()
    {
        if(!spherecast) radius = -1f;
        constantGroundSpeed = constantGroundSpeed || Mathf.Approximately(speed, endSpeed);
        _groundSpeed = speed;
        if (constantGroundSpeed)
        {
            changeSpeedTime = 0;
            endSpeed = _groundSpeed;
        }
        _acceleration = constantGroundSpeed? 0 : (endSpeed - _groundSpeed) / changeSpeedTime;
        if (aimType != AimType.Transform) followTarget = false;
    }

    private void CalculateRoute(Vector3 targetPos)
    {
        float yDiff = targetPos.y - transform.position.y;
        float estimatedFlightTime;
        
        if (constantGroundSpeed)
        { 
            estimatedFlightTime = Vector3.Distance( new Vector3(targetPos.x,0,targetPos.z), 
                new Vector3(transform.position.x,0,transform.position.z)) / _groundSpeed;
        }
        else
        {
            float d = Vector3.Distance(new Vector3(targetPos.x, 0, targetPos.z), new Vector3(transform.position.x, 0, transform.position.z));
            float delta = d - 0.5f * (_groundSpeed + endSpeed) * changeSpeedTime;
            estimatedFlightTime = delta > 0
                ?  delta / endSpeed + changeSpeedTime 
                : CalculateTime(_groundSpeed, _acceleration, d);

        }
        _ySpeed = estimatedFlightTime <= 0 
            ? 0 
            : gravity > 0 
                ? yDiff / estimatedFlightTime + 0.5f * gravity * estimatedFlightTime 
                : yDiff / estimatedFlightTime;
        var direction = new Vector3(_groundDirection.x, _ySpeed / _groundSpeed, _groundDirection.z).normalized;
        
        if(faceMoveDirection) transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
        groundSpeed = _groundSpeed,
        endGroundSpeed = endSpeed,
        acceleration = _acceleration,
        ySpeed = _ySpeed,
        gravity = gravity,
        faceMoveDirection = faceMoveDirection
    };

    public void Read(ProjectileMoveResult moveResult)
    {
        _groundSpeed = moveResult.groundSpeed;
        _ySpeed = moveResult.ySpeed;
        _groundDirection = moveResult.groundDirection;
    }
    
    
    public static Vector3 TrackTarget(Vector3 target, Vector3 me, Vector3 groundDirection, float trackTargetMaxRadiansPerSecond, float deltaTime)
    {
        Vector3 targetDirection = target - me;
        targetDirection.y = 0;
       return Vector3.RotateTowards(groundDirection, targetDirection.normalized, 
            trackTargetMaxRadiansPerSecond/deltaTime,2) ;
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
                if(newGroundSpeed > endGroundSpeed) newGroundSpeed = endGroundSpeed;
            }
        }
        // Decelerate
        else if (acceleration < 0)
        {
            if (newGroundSpeed > endGroundSpeed)
            {
                newGroundSpeed += acceleration * deltaTime;
                if(newGroundSpeed < endGroundSpeed) newGroundSpeed = endGroundSpeed;
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

    private void RenameComponent()
    {
        if(isPooling) this.SetName($"Projectile ({uniqueId})" );
        else this.SetName("Projectile");
    }

    private void OnDrawGizmosSelected()
    {
        if(spherecast && radius > 0f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}