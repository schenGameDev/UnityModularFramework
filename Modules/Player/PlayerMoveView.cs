using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Modules.Ability;
using ModularFramework.Modules.Input;
using Unity.Mathematics;
using UnityEngine;
using UnityModularFramework.Modules.Player;
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
    
    [SerializeField,Tooltip("Distinguish landing of a jump and an uncontrollable fall"), Rename("Fall calculated separately")] 
    private bool useFallProcessor;
    [SerializeReference, ShowField(nameof(useFallProcessor))] 
    private Fall fallProcessor;

    [ReadOnly] public JumpState jumpState = JumpState.GROUNDED; // may be changed by game events
    
    public enum JumpState
    {
        GROUNDED, JUMPING, FALLING
    }
    
    [Header("Event Channel")]
    [SerializeField] private EventChannel<bool> jumpChannel;
    [SerializeField] private EventChannel<Vector2> moveChannel, viewChannel;
    
    [SerializeField,Self] private CharacterController characterController;
    
    private Vector3 _viewDirection;
    private Vector3 _velocity;
    private Vector3 _moveDirection;
    public bool isSprinting;
    private Autowire<InputSystemSO> _inputSystem = new();

    private void OnEnable()
    {
        LinkEventChannels();
        _moveDirection = Vector3.zero;
        _velocity = Vector3.zero;
        jumpState = JumpState.GROUNDED;
    }

    private void OnDisable()
    {
        CleanUp();
    }

    private void Update()
    {
        Look();
    }
    
    private void FixedUpdate() {
        Move();
        ShowShadow();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    public void Stop() => _moveDirection = Vector3.zero;
    
    private void Look() {
        if(_viewDirection == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(_viewDirection, Vector3.up);

    }
    
    private Vector3 Move()
    {
        var displacement = GetGroundVelocity();
        bool isGrounded = characterController.isGrounded;
        if (jumpProcessor != null)
        {
            bool startJumpThisFrame = jumpProcessor.Prepare();
            var delta = jumpProcessor.GetVerticalVelocity(transform.position.y, isGrounded);

            switch (jumpProcessor.IsJumping)
            {
                case false when isGrounded:
                    jumpState = JumpState.GROUNDED;
                    break;
                case false when fallProcessor != null:
                    jumpState = JumpState.FALLING;
                    fallProcessor.Prepare();
                    delta = fallProcessor.GetVerticalVelocity(transform.position.y, isGrounded);
                    break;
                default:
                    jumpState = JumpState.JUMPING;
                    break;
            }
            displacement += delta;
        }
        else if (fallProcessor != null)
        {
            fallProcessor.Prepare();
            displacement += fallProcessor.GetVerticalVelocity(transform.position.y, isGrounded);
            jumpState = isGrounded ? JumpState.GROUNDED : JumpState.FALLING;
        }
        else
        {
            displacement += new Vector3(0,-2, 0); // always press to ground
            jumpState = isGrounded ? JumpState.GROUNDED : JumpState.FALLING;
        }
        var oldPos = transform.position;
        var collisionFlags = characterController.Move(displacement);
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
        // if(other.gameObject.IsInLayer(groundLayer)) return;
        
        // Debug.Log($"{name} Hit {other.gameObject.tag}");
    }

    private bool IsStuck => characterController.velocity is { x: 0, z: 0 };
    
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
    
    private float GroundAcceleration() =>  jumpState switch
    {
        JumpState.JUMPING => jumpProcessor.GroundAccelMidair(acceleration),
        JumpState.FALLING => fallProcessor.GroundAccelMidair(acceleration),
        _ => acceleration
    };
    private float GroundDeceleration() =>  jumpState switch {
        JumpState.JUMPING => jumpProcessor.GroundDecelMidair(deceleration),
        JumpState.FALLING => fallProcessor.GroundDecelMidair(deceleration),
        _ => deceleration
    };

    private float GetMaxSpeed()
    {
        var ms = isSprinting? maxSprintSpd : maxSpeed;
        return jumpState switch
        {
            JumpState.JUMPING => jumpProcessor.MaxGroundSpeedMidair(ms),
            JumpState.FALLING => fallProcessor.MaxGroundSpeedMidair(ms),
            _ => ms
        };
    } 

    // private void OnDrawGizmos()
    // {
    //     if (jumpProcessor is null or { IsJumping: false }) return;
    //     Gizmos.color = Color.cyan;
    //     var pos = PhysicsUtil.FindGroundPosition(transform.position);
    //     Gizmos.DrawSphere(pos, 0.3f);
    // }
    
    private ImpactZoneIndicator _shadowIndicator;
    private void ShowShadow()
    {
        if (jumpState is JumpState.JUMPING or JumpState.FALLING)
        {
            if (_shadowIndicator == null)
            {
                _shadowIndicator = PrefabPool<ImpactZoneIndicator>.Get();
            }
            _shadowIndicator.ShowInLocalCoordinate(transform, Vector3.zero, 0.5f, Color.gray2);
        }
        // var groundPos = PhysicsUtil.FindGroundPosition(transform.position);
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

    private void CleanUp()
    {
        UnlinkEventChannels();
        if (_shadowIndicator != null)
        {
            PrefabPool<ImpactZoneIndicator>.Release(_shadowIndicator);
            _shadowIndicator = null;
        }
        jumpProcessor?.ResetState();
        fallProcessor?.ResetState();
    }

    private void OnJump(bool isJumping) // button down or up
    {
        jumpProcessor?.StartJump(isJumping);
    }
    
    private void OnMove(Vector2 input)
    {
        _moveDirection = _inputSystem.Get().CameraDirectionToWorldSpace(input).normalized;
    }
    
    private void OnView(Vector2 input)
    {
        _viewDirection = _inputSystem.Get().GetViewWorldDirection(input);
    }
#if UNITY_EDITOR
    private void OnValidate() => this.ValidateRefs();
#endif
}