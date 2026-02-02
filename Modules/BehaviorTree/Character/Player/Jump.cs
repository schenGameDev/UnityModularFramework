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
    
    private readonly float _jumpDropGMultiplier = 1.2f;
    private readonly float _jumpEarlyReleaseGMultiplier = 1.4f;
    private bool _jumpThisFrame;
    private bool _cancelLastJump;
    private bool _sameJump;
    private int _usedJumps;
    
    public override void StartJump(bool isStarted)
    {
        if (isStarted)
        {
            _isJumpPressedLately.Record();
            _cancelLastJump = false;
            _sameJump = false;
        }
        else _cancelLastJump = true;
    }

    public override bool Prepare()
    {
        _jumpThisFrame = !IsJumping && _isJumpPressedLately && (_isGroundedLately || (_usedJumps < allowedJumps && _usedJumps>0));
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
        var g = GetGravity();
        
        if (_jumpThisFrame)
        {
            YSpeed = jumpSpd; // double-jump second jump same as jump from group
        } else if (isGrounded && DroppingOrOnGround)
        {
            IsJumping = false;
            YSpeed = -0.1f; // press to ground
            _usedJumps = 0;
        }
        else if(YSpeed > -maxDropSpd)
        {
            YSpeed += g * Time.fixedDeltaTime;
            if(YSpeed < -maxDropSpd) YSpeed = -maxDropSpd; // max drop speed
        }

        return new Vector3(0, YSpeed * Time.fixedDeltaTime + 0.5f * g * Time.fixedDeltaTime * Time.fixedDeltaTime, 0);
    }

    private readonly EventMemory _isGroundedLately = new (0.1f);
    private readonly EventMemory _isJumpPressedLately = new (0.1f);
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
                return - gravity * _jumpDropGMultiplier;
            }

            if (_sameJump && _cancelLastJump && Rising && YSpeed <= 0.8f * jumpSpd) // cancel jump cause lower peak, but there's a min height
            {
                
                return - gravity * _jumpEarlyReleaseGMultiplier;
            }
        }

        return - gravity;
    }
    
    private bool Rising => !DroppingOrOnGround;
}