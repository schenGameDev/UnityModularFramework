using EditorAttributes;
using ModularFramework;
using ModularFramework.Utility;
using Unity.Mathematics;
using UnityEngine;
using Void = EditorAttributes.Void;

[DisallowMultipleComponent,RequireComponent(typeof(CharacterController))]
public class PlayerMoveView : MonoBehaviour
{
    
    [FoldoutGroup("Move", nameof(maxSpeed), nameof(maxSprintSpd), nameof(acceleration), nameof(deceleration))]
    [SerializeField] private Void moveGroupHolder;
    
    [SerializeField, HideInInspector, Min(0)] private float maxSpeed = 10;
    [SerializeField, HideInInspector, Min(0)] private float maxSprintSpd = 15;
    [SerializeField, HideInInspector, Min(0)] private float acceleration = 3;
    [SerializeField, HideInInspector, Min(0)] private float deceleration = 6;
    
    [SerializeReference, SubclassSelector] 
    private JumpProcessor jumpProcessor;
    
    [Header("Event Channel")]
    [SerializeField] private EventChannel<bool> jumpChannel;
    [SerializeField] private EventChannel<Vector2> moveChannel, viewChannel;
    
    
    private CharacterController _characterController;
    
    private Vector3 _viewDirection;
    private Vector3 _velocity;
    private Vector3 _moveDirection;
    public bool isSprinting;
    private Autowire<InputSystemSO> _inputSystem;

    protected void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        LinkEventChannels();
    }

    private void OnDisable()
    {
        UnlinkEventChannels();
    }

    private void Update()
    {
        Look();
    }
    
    private void FixedUpdate() {
        Move();
    }

    public void Stop() => _moveDirection = Vector3.zero;
    
    private void Look() {
        if(_viewDirection == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(_viewDirection, Vector3.up);

    }
    
    private Vector3 Move()
    {
        var displacement = GetGroundVelocity();
        if (jumpProcessor != null)
        {
            bool startJumpThisFrame = jumpProcessor.Prepare();
            displacement += jumpProcessor.GetVerticalVelocity(transform.position.y, _characterController.isGrounded);
        }
        else
        {
            displacement += new Vector3(0,-2, 0);
        }
        var oldPos = transform.position;
        var collisionFlags = _characterController.Move(displacement);
        var actualDisplacement = displacement;
        if ((collisionFlags & CollisionFlags.Sides) != 0)
        {
            //Debug.Log($"{name} Hit Wall");
            actualDisplacement = transform.position - oldPos;
            var faceWallDir = displacement - actualDisplacement;
            faceWallDir.y = 0;
            if(faceWallDir.sqrMagnitude > 0.0001f) _velocity -= Vector3.Project(_velocity, faceWallDir);
            if (jumpProcessor != null)
            {
                bool hitHead = math.abs(actualDisplacement.y)<0.01f && math.abs(displacement.y)> 0.01f;
                if (hitHead)
                {
                    jumpProcessor.YSpeed = 0;
                }
            }
            
        }
        return actualDisplacement;
    }

    private void OnControllerColliderHit(ControllerColliderHit other) // object hit during movement, not including other physics impact
    {
        // if(other.gameObject.layer == groundLayer) return;
        
        // Debug.Log($"{name} Hit {other.gameObject.tag}");
    }

    private bool IsStuck => _characterController.velocity is { x: 0, z: 0 };
    
    private Vector3 GetGroundVelocity()
    {
        bool isIdle = _moveDirection == Vector3.zero;
        bool isTurnBack = !isIdle && _velocity != Vector3.zero && Vector3.Angle(_moveDirection, _velocity) > 90;
        Vector3 oldVelocity = _velocity;
        if (isIdle || isTurnBack)
        {
            _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, GroundDeceleration() * Time.fixedDeltaTime);
        }
        else
        {
            var maxGroundSpeed = GetMaxSpeed();
            var targetDirVec = Vector3.Project(_velocity, _moveDirection);
            var targetDirVelocity = targetDirVec.sqrMagnitude < maxGroundSpeed * maxGroundSpeed? 
                targetDirVec + GroundAcceleration() * Time.fixedDeltaTime * _moveDirection : targetDirVec;
            
            var normVec = _velocity - targetDirVec;
            var normDirVelocity = normVec == Vector3.zero? Vector3.zero : 
                Vector3.MoveTowards(normVec, Vector3.zero, GroundDeceleration() * Time.fixedDeltaTime);
            _velocity = Vector3.ClampMagnitude(targetDirVelocity + normDirVelocity, maxGroundSpeed);
        }
        return 0.5f * Time.fixedDeltaTime * (oldVelocity + _velocity);
        // _velocity * Time.fixedDeltaTime  + (maxSpdReached? 0 : 0.5f * acceleration * Time.fixedDeltaTime * Time.fixedDeltaTime)
    }
    
    private float GroundAcceleration() =>  jumpProcessor is { IsJumping: true }? jumpProcessor.GroundAccelMidair(acceleration) : acceleration;
    private float GroundDeceleration() => jumpProcessor is { IsJumping: true }? jumpProcessor.GroundAccelMidair(deceleration) : deceleration;

    private float GetMaxSpeed()
    {
        var ms = isSprinting? maxSprintSpd : maxSpeed;
        return jumpProcessor is { IsJumping: true }? jumpProcessor.MaxGroundSpeedMidair(ms) : ms;
    } 

    private void OnDrawGizmos()
    {
        if (jumpProcessor is null or { IsJumping: false }) return;
        Gizmos.color = Color.cyan;
        var pos = PhysicsUtil.FindGroundPosition(transform.position);
        Gizmos.DrawSphere(pos, 0.3f);
    }
    
    private void LinkEventChannels()
    {
        moveChannel?.AddListener(OnMove);
        viewChannel?.AddListener(OnView);
        jumpChannel?.AddListener(OnJump);
    }
    
    private void UnlinkEventChannels()
    {
        moveChannel?.RemoveListener(OnMove);
        viewChannel?.RemoveListener(OnView);
        jumpChannel?.RemoveListener(OnJump);
    }

    private void OnJump(bool isJumping)
    {
        if(jumpProcessor is not null) jumpProcessor.StartJump(isJumping);
    }
    
    private void OnMove(Vector2 input)
    {
        _moveDirection = _inputSystem.Get().CameraDirectionToWorldSpace(input).normalized;
    }
    
    private void OnView(Vector2 input)
    {
        _viewDirection = _inputSystem.Get().GetViewWorldDirection(input);
    }
}