using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using ModularFramework.Modules.Ability;
using ModularFramework.Modules.Input;
using Unity.Mathematics;
using UnityEngine;
using Void = EditorAttributes.Void;

namespace UnityModularFramework.Modules.Player
{
    [DisallowMultipleComponent,RequireComponent(typeof(CharacterController))]
    public class PlayerMoveView : MonoBehaviour
    {
        
        [FoldoutGroup("Move", nameof(maxSpeed), nameof(acceleration), nameof(deceleration))]
        [SerializeField] private Void moveGroupHolder;
        
        [SerializeField, HideInInspector, Min(0)] private float maxSpeed = 10;
        [SerializeField, HideInInspector, Min(0)] private float acceleration = 3;
        [SerializeField, HideInInspector, Min(0)] private float deceleration = 6;
        
        [FoldoutGroup("Sprint", nameof(maxSprintSpd), nameof(sprintConsumption), nameof(isSprinting))]
        [SerializeField] private Void sprintGroupHolder;
        [SerializeField, HideInInspector, Min(0)] private float maxSprintSpd = 15;
        [SerializeField, HideProperty] private Player.StatusConsumptionDef sprintConsumption;
        [HideInInspector] public bool isSprinting;
        
        
        [FoldoutGroup("Jump", nameof(jumpConsumption), nameof(jumpProcessor), nameof(jumpState))]
        [SerializeField] private Void jumpGroupHolder;
        [SerializeField, HideProperty] private Player.StatusConsumptionDef jumpConsumption;
        [SerializeReference, SubclassSelector, HideProperty] private JumpProcessor jumpProcessor;
        [ReadOnly,HideProperty] public JumpState jumpState = JumpState.GROUNDED; // may be changed by game events
        
        [ToggleGroup("Fall calculated separately", nameof(fallProcessor))]
        [SerializeField,Tooltip("Distinguish landing of a jump and an uncontrollable fall")] 
        private bool useFallProcessor;
        [SerializeReference, HideProperty] private Fall fallProcessor;
        
        [SerializeField] private bool isFallDamage;

        [SerializeField, ShowField(nameof(isFallDamage)), Min(0)]
        private float fallDamageHeightThreshold = 1;
        [SerializeField, ShowField(nameof(isFallDamage))] 
        private float fallDamageModifier = 0.1f;
        
        [SerializeField] private bool rotateWithView = true;
        
        [Header("Event Channel")]
        [SerializeField] private BoolEventChannelSO jumpChannel;
        [SerializeField] private BoolEventChannelSO sprintChannel;
        [SerializeField] private Vector2EventChannelSO moveChannel, viewChannel;
        
        [SerializeField,Self] private CharacterController characterController;
        [SerializeField,Self] private Player player;
        
        private Vector3 _viewDirection;
        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private float _fallHeight;
        
        private Autowire<InputSystemSO> _inputSystem = new();
        private FallHeightProcessor _fallHeightProcessor;
        private float _knockBackTimer;
        private Vector3 _knockBackVelocity;
        private bool IsKnockBacking => _knockBackTimer > 0;
        private bool IsStuck => characterController.velocity is { x: 0, z: 0 };

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

        private void Awake()
        {
            if (isFallDamage)
            {
                _fallHeightProcessor = useFallProcessor
                        ? new FallHeightProcessor(JumpState.FALLING, true)
                        : new FallHeightProcessor(JumpState.GROUNDED, false);
                _fallHeightProcessor.OnFallHeight += OnFall;
            }
        }

        private void Update()
        {
            Look();
        }
        
        private void FixedUpdate() {
            Move(Time.fixedDeltaTime);
            MonitorStatusConsumption(Time.fixedDeltaTime);
            ShowShadow();
        }

        private void OnDestroy()
        {
            CleanUp();
        }

        public void Stop() => _moveDirection = Vector3.zero;
        
        private void Look() {
            if (!rotateWithView) return;
            if(_viewDirection == Vector3.zero) return;
            transform.rotation = Quaternion.LookRotation(_viewDirection, Vector3.up);

        }
        
        private Vector3 Move(float dt)
        {
            var displacement = GetGroundVelocity(dt);
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

                if (IsKnockBacking)
                {
                    CancelKnockBack();
                }
                
            }
            _fallHeightProcessor?.Update(jumpState, transform.position.y);
            return actualDisplacement;
        }
        
        public void ApplyExternalVelocity(Vector3 externalVelocity, float duration)
        {
            _knockBackTimer = duration;
            _knockBackVelocity = externalVelocity;
        }
        
        private void CancelKnockBack()
        {
            _knockBackVelocity = Vector3.zero;
            player.KnockBackComplete();
        }

        private void OnControllerColliderHit(ControllerColliderHit other) // object hit during movement, not including other physics impact
        {
            // if(other.gameObject.IsInLayer(groundLayer)) return;
            
            // Debug.Log($"{name} Hit {other.gameObject.tag}");
        }
        
        
        private Vector3 GetGroundVelocity(float dt)
        {
            if (IsKnockBacking)
            {
                return _knockBackVelocity * dt;
            }
            
            bool isIdle = _moveDirection == Vector3.zero;
            bool isTurnBack = !isIdle && _velocity != Vector3.zero && Vector3.Angle(_moveDirection, _velocity) > 90;
            Vector3 oldVelocity = _velocity;
            if (isIdle || isTurnBack)
            {
                _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, GroundDeceleration() * dt);
            }
            else
            {
                var maxGroundSpeed = GetMaxSpeed();
                var targetDirVec = Vector3.Project(_velocity, _moveDirection);
                var targetDirVelocity = targetDirVec.sqrMagnitude < maxGroundSpeed * maxGroundSpeed? 
                    targetDirVec + GroundAcceleration() * dt * _moveDirection : targetDirVec;
                
                var normVec = _velocity - targetDirVec;
                var normDirVelocity = normVec == Vector3.zero? Vector3.zero : 
                    Vector3.MoveTowards(normVec, Vector3.zero, GroundDeceleration() * dt);
                _velocity = Vector3.ClampMagnitude(targetDirVelocity + normDirVelocity, maxGroundSpeed);
            }
            return 0.5f * dt * (oldVelocity + _velocity);
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
            sprintChannel?.AddListener(OnSprint);
        }
        
        private void UnlinkEventChannels()
        {
            moveChannel?.RemoveListener(OnMove);
            viewChannel?.RemoveListener(OnView);
            jumpChannel?.RemoveListener(OnJump);
            sprintChannel?.RemoveListener(OnSprint);
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

        private void OnFall(float height)
        {
            if (height < fallDamageHeightThreshold) return; 
            player.TakeDamage(fallDamageModifier * height, DamageType.Physical, transform);  
        }

        private void OnJump(bool isJumping) // button down or up
        {
            if (isJumping && !player.IsStatusEnough(jumpConsumption.key, jumpConsumption.startAmount))
            {
                return;
            }
            
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
        
        private void OnSprint(bool sprinting)  // button down or up
        {
            if (sprinting && !player.IsStatusEnough(sprintConsumption.key, sprintConsumption.startAmount))
            {
                return;
            }
            isSprinting = sprinting;
        }

        private void MonitorStatusConsumption(float dt)
        {
            if (isSprinting 
                && sprintConsumption.NeedSustain
                && player.ConsumeStatus(sprintConsumption.key, sprintConsumption.sustainAmount * dt) <= 0)
            {
                OnSprint(false);
            }
            
            if (jumpState is JumpState.JUMPING 
                && jumpConsumption.NeedSustain
                && player.ConsumeStatus(jumpConsumption.key, jumpConsumption.sustainAmount * dt) <= 0)
            {
                jumpProcessor?.StartJump(false);
            }
        }
    #if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
    #endif
        
        public enum JumpState
        {
            GROUNDED, JUMPING, FALLING
        }
    }
}