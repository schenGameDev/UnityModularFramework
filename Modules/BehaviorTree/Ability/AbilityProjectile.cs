using System;
using EditorAttributes;
using UnityEngine;
using UnityTimer;

[RequireComponent(typeof(Collider))]
public class AbilityProjectile : MonoBehaviour
{
    [SerializeField] private bool accelerate = false;
    [ShowField(nameof(accelerate)),SerializeField,Clamp(0,999,0,999)] private Vector2 startEndSpeed;
    [ShowField(nameof(accelerate)),SerializeField,Suffix("/s"),Min(0)] private float accelerateTime = 1f;
    [HideField(nameof(accelerate)),Min(0),Tooltip("Ground speed, not include y axis"),SerializeField] private float initialSpeed;
    
    [SerializeField,Min(0),Suffix("/s")] private float gravityAcceleration = 0f;
    
    [SerializeField,Min(0),Suffix("s")] private float lifetime;
    [SerializeField] private bool blockByBuilding;
    [SerializeField, ShowField(nameof(blockByBuilding))] private bool damageBuilding;
    [SerializeField] private bool followTarget;
    [SerializeField,Tooltip("spawn on impact")] 
    private AbilityGroundEffect groundEffectPrefab;
    
    [HelpBox("Keep it empty, it will get assigned at runtime. Unless you are testing")] 
    [SerializeReference] public AbilitySO ability;
    
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private GameObject hitVfx;
    
    private CountdownTimer _timer;
    private IDamageable _target;
    private Vector3 _targetPosition;
    private float _speed;
    private float _acceleration;
    private Action _onComplete;
    private float _ySpeed;
    [SerializeField] private bool live;
    
    private void Start()
    {
        if(!live) return;
        if (accelerate)
        {
            _speed = startEndSpeed.x;
            _acceleration = (startEndSpeed.y - startEndSpeed.x) / accelerateTime;
            if (gravityAcceleration > 0)
            {
                float d = Vector3.Distance(new Vector3(_targetPosition.x, 0, _targetPosition.z),
                    new Vector3(transform.position.x, 0, transform.position.z)) -  0.5f * (startEndSpeed.x + startEndSpeed.y) * accelerateTime;

                float estimatedFlightTime = d / startEndSpeed.y + accelerateTime;
                float yDiff = _targetPosition.y - transform.position.y;
                _ySpeed = yDiff / estimatedFlightTime + 0.5f * gravityAcceleration * estimatedFlightTime;
            }
           
        }
        else
        {
            _speed = initialSpeed;
            if (initialSpeed <= 0)
            {
                Debug.LogWarning($"Projectile {name} must have positive initialSpeed");
            }
            _acceleration = 0;
            if (gravityAcceleration > 0)
            {
                float estimatedFlightTime = Vector3.Distance( new Vector3(_targetPosition.x,0,_targetPosition.z), new Vector3(transform.position.x,0,transform.position.z)) / _speed;
                float yDiff = _targetPosition.y - transform.position.y;
                _ySpeed = yDiff / estimatedFlightTime + 0.5f * gravityAcceleration * estimatedFlightTime;
            }
            
        }
        
        if (lifetime > 0)
        {
            _timer = new CountdownTimer(lifetime);
            _timer.OnTick = Fly;
            _timer.OnTimerStop = () => Destroy(gameObject);
            _timer.Start();
        }
        else
        {
            Debug.LogWarning("Projectile cannot has 0 lifetime");
            Destroy(gameObject);
        }
        
    }

    private void Fly()
    {
        if (followTarget && _target != null && _targetPosition != _target.Transform.position) // update target position
        {
            _targetPosition = _target.Transform.position;
        }
        
        Vector3 groundDirection = _targetPosition - transform.position;
        groundDirection.y = 0;
        transform.position += DeltaMovement(groundDirection);

        // Optional: Rotate to face the direction of movement
        if (groundDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(_targetPosition - transform.position);
        }

        // Check if reached target
        if (Vector3.SqrMagnitude(transform.position- _targetPosition) < 0.1f)
        {
            if(_target == null) Arrive(null);
            else
            {
                Arrive(Vector3.SqrMagnitude(transform.position- _target.Transform.position) < 0.1f? _target : null);
            }
            
        }
    }

    private Vector3 DeltaMovement(Vector3 groundDirection)
    {
        float groundSpd = _speed;
        float gravitySpd = _ySpeed;
        // Accelerate
        if (_acceleration == 0)
        {
            
        }
        else if (startEndSpeed.x < startEndSpeed.y &&  groundSpd < startEndSpeed.y)
        {
            groundSpd += _acceleration * _timer.DeltaTime;
            if(groundSpd > startEndSpeed.y) groundSpd = startEndSpeed.y;
        }
        // Decelerate
        else if (startEndSpeed.x > startEndSpeed.y &&  groundSpd > startEndSpeed.y)
        {
            groundSpd += _acceleration * _timer.DeltaTime;
            if(groundSpd < startEndSpeed.y) groundSpd = startEndSpeed.y;
        }
        
        // Gravity effect
        if (gravityAcceleration > 0)
        {
            gravitySpd -= gravityAcceleration * _timer.DeltaTime;
        }
        
        var delta = groundDirection * 0.5f * (_speed + groundSpd) * Time.deltaTime +
                    Vector3.up * 0.5f * (gravitySpd + _ySpeed) * Time.deltaTime;
        
        _speed = groundSpd;
        _ySpeed = gravitySpd;
        return delta;
    }
    
    private bool _arrived;
    private void Arrive(IDamageable target)
    {
        if (target != null)
        {
            if(_arrived) return;
            _arrived = true;
        }
        // sfx vfx
        _timer?.Stop();
        if (groundEffectPrefab)
        {
            var groundEffect = Instantiate(groundEffectPrefab, transform.position, transform.rotation);
            groundEffect.Setup(ability, _onComplete);
        }
        else
        {
            ability.Execute(target, _onComplete);
        }
        
        Destroy(gameObject, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Arrive(other.GetComponent<IDamageable>());
            return;
        }
        if(blockByBuilding && other.CompareTag("Building"))
        {
            Arrive(damageBuilding? other.GetComponent<IDamageable>() : null);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Arrive(null);
        }
    }

    public void Setup(AbilitySO ability,IDamageable target, Action onComplete)
    {
        _target = target;
        _targetPosition = target.Transform.position + new Vector3(0, 1, 0);
        this.ability = ability;
        _onComplete = onComplete;
        live = true;
    }
    
    public void Setup(AbilitySO ability,Vector3 targetPosition, Action onComplete)
    {
        _target = null;
        _targetPosition = targetPosition;
        this.ability = ability;
        _onComplete = onComplete;
        live = true;
    }

}
