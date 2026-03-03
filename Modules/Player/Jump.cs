using System;
using ModularFramework.Commons;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public class Jump : JumpProcessor
{
    // freeze movement if jump when still, decelerate to top, accelerate drop, air drag increase with speed
    [SerializeField, Min(0)] private float maxDropSpd = 10;
    [SerializeField, Min(0)] private float gravity = 10;
    [SerializeField, Min(0)] private float jumpSpd = 5;
    [SerializeField, Range(0, 1)] private float accelerationModifier = 1; // move slower midair
    [SerializeField, Range(0, 1)] private float maxSpdModifier = 1;
    [SerializeField] private int allowedJumps = 1;

    private const float JUMP_DROP_G_MULTIPLIER = 1.2f;
    private const float JUMP_EARLY_RELEASE_G_MULTIPLIER = 1.4f;

    private bool _jumpThisFrame;
    private bool _holdingJump;
    private bool _sameJump;
    private int _usedJumps;
    
    private readonly EventMemory _isGroundedLately = new (0.1f);
    private readonly EventMemory _isJumpPressedLately = new (0.1f);
    
    public override float GroundAccelMidair(float groundAccel)
    {
        return accelerationModifier * groundAccel;
    }

    public override float GroundDecelMidair(float groundDecel)
    {
        return accelerationModifier * groundDecel;
    }

    public override float MaxGroundSpeedMidair(float maxGroundSpeed)
    {
        return maxSpdModifier * maxGroundSpeed;
    }
    
    public override void StartJump(bool isStarted)
    {
        if (isStarted)
        {
            _isJumpPressedLately.Record();
            _holdingJump = true;
            _sameJump = false;
        }
        else _holdingJump = false;
    }

    public override bool Prepare()
    {
        _jumpThisFrame = _isJumpPressedLately && (_isGroundedLately || (_usedJumps < allowedJumps && _usedJumps>0));
        if (!_jumpThisFrame) return false;
        _isJumpPressedLately.ResetState();
        IsJumping = true;
        _usedJumps+=1;
        _sameJump = true;
        return true;
    }

    public override Vector3 GetVerticalVelocity(float yValue, bool isGrounded)
    {
        RecordGrounded(isGrounded);
        
        if (!IsJumping) return Vector3.zero;
        
        var g = GetGravity();
        
        if (_jumpThisFrame)
        {
            YSpeed = jumpSpd; // double-jump second jump same as jump from group
            return new Vector3(0, YSpeed * Time.fixedDeltaTime, 0);
        } 
        
        if (isGrounded && DroppingOrOnGround)
        {
            IsJumping = false;
            _sameJump = false;
            YSpeed = -0.1f; // press to ground
            _usedJumps = 0;
            return new Vector3(0, YSpeed , 0);
        }

        if (YSpeed <= -maxDropSpd)
        {
            YSpeed = -maxDropSpd;
            return new Vector3(0, YSpeed * Time.fixedDeltaTime, 0);
        }
        
        var oldYSpeed = YSpeed;
        YSpeed += g * Time.fixedDeltaTime;
        if (YSpeed >= -maxDropSpd) // accelerate faster
        {
            return new Vector3(0, (YSpeed + oldYSpeed) * 0.5f * Time.fixedDeltaTime,0);
        }

        var accelTime = (YSpeed - oldYSpeed) / g;
        return new Vector3(0, oldYSpeed * accelTime + 0.5f * g * accelTime * accelTime - maxDropSpd * (Time.fixedDeltaTime - accelTime), 0);
    }
    
    private void RecordGrounded(bool isGroundedThisFrame) {
        if (isGroundedThisFrame)  _isGroundedLately.Record();
    }

    private float GetGravity() // < 0
    {
        if (IsJumping && !_jumpThisFrame) 
        {
            if (math.abs(YSpeed) < 0.1f) { // more airtime at peak of jump
                return - gravity * 0.5f; 
            }

            if (DroppingOrOnGround) // fast falling
            {
                return - gravity * JUMP_DROP_G_MULTIPLIER;
            }

            if (_sameJump && !_holdingJump && Rising && YSpeed <= 0.8f * jumpSpd) // cancel jump cause lower peak, but there's a min height
            {
                
                return - gravity * JUMP_EARLY_RELEASE_G_MULTIPLIER;
            }
        }

        return - gravity;
    }
    
    private bool Rising => !DroppingOrOnGround;

    public override void ResetState()
    {
        base.ResetState();
        _jumpThisFrame = false;
        _holdingJump = false;
        _sameJump = false;
        _usedJumps = 0;
        _isGroundedLately.ResetState();
        _isJumpPressedLately.ResetState();
    }
}