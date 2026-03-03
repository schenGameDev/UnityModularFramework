using System;
using UnityEngine;

[Serializable]
public class Jetpack : JumpProcessor
{
    private const float UNINITIALIZED_GROUND_HEIGHT = -100;
    [SerializeField, Min(0)] private float maxDropSpd = 10;
    [SerializeField, Min(0)] private float gravity = 10;
    [SerializeField, Min(0)] private float jetAcceleration = 15;
    [SerializeField, Min(0)] private float jetMaxYSpeed = 5;
    [SerializeField, Min(0),Tooltip("relative to start point height")] 
    private float maxHeight = 15;
    [SerializeField, Range(0, 1)] private float accelerationModifier = 0.5f; // move slower midair
    [SerializeField, Range(0, 1)] private float maxSpdModifier = 0.5f;
    private bool _jumpThisFrame;
    private float _lastGroundHeight = UNINITIALIZED_GROUND_HEIGHT;
    private bool _jetting;
    
    public override void StartJump(bool isStarted)
    {
        _jumpThisFrame = isStarted;
        _jetting = isStarted;
    }

    public override bool Prepare()
    {
        if (_jumpThisFrame)
        {
            if (IsJumping) _jumpThisFrame = false;
            else
            {
                IsJumping = true;
                return true;
            }
        }
        
        return false;
    }
    
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

    public override Vector3 GetVerticalVelocity(float yValue, bool isGrounded)
    {
        if (!IsJumping) return Vector3.zero;
        
        if (_lastGroundHeight == UNINITIALIZED_GROUND_HEIGHT) _lastGroundHeight = yValue; // first time prepare is called, record the ground height
        
        if (isGrounded && DroppingOrOnGround && !_jetting)
        {
            YSpeed = -0.1f; // press to ground
            _lastGroundHeight = yValue;
            IsJumping = false;
            return new Vector3(0, YSpeed, 0);
        }

        var oldYSpeed = YSpeed;
        if (_jetting) // upwards
        {
            if (yValue - _lastGroundHeight > maxHeight)
            {
                return Vector3.zero;
            }
            
            if (YSpeed >= jetMaxYSpeed)
            {
                YSpeed = jetMaxYSpeed;
                return new Vector3(0, YSpeed * Time.fixedDeltaTime, 0);
            }
            
            YSpeed = Mathf.Clamp(YSpeed + ( - gravity + jetAcceleration) * Time.fixedDeltaTime, -maxDropSpd, jetMaxYSpeed);
            return new Vector3(0, (YSpeed + oldYSpeed) * 0.5f * Time.fixedDeltaTime,0);
        } 
        if(YSpeed > -maxDropSpd) // falling
        {
            YSpeed = Mathf.Clamp(YSpeed - gravity * Time.fixedDeltaTime, -maxDropSpd, jetMaxYSpeed);
            
            return new Vector3(0, (YSpeed + oldYSpeed) * 0.5f * Time.fixedDeltaTime,0);
        }
        YSpeed = - maxDropSpd;
        return new Vector3(0, YSpeed * Time.fixedDeltaTime, 0);
    }

    public override void ResetState()
    {
        base.ResetState();
        _jumpThisFrame = false;
        _lastGroundHeight = UNINITIALIZED_GROUND_HEIGHT;
        _jetting = false;
    }
}