using System;
using EditorAttributes;
using UnityEngine;

[Serializable]
public class Jetpack : JumpProcessor
{
    private const float UNINITIALIZED_GROUND_HEIGHT = -100;
    [SerializeField, Min(0)] private float maxDropSpd = 10;
    [SerializeField, Min(0)] private float gravity = 3; // jet gravity, different from fall gravity
    [SerializeField, Min(0)] private float shortPressJetHeight = 2;
    [SerializeField, Min(0), Suffix("s")] private float shortPressJetTime = 1;
    [SerializeField, Min(0), Suffix("s")] private float flyLongPressDelay = 0.3f;
    [SerializeField, Min(0), Suffix("/s")] private float longPressJetSpeed = 5;
    
    [SerializeField, Min(0),Tooltip("relative to start point height")] 
    private float maxHeight = 15;
    [SerializeField, Range(0, 1)] private float accelerationModifier = 0.7f; // move slower midair
    [SerializeField, Range(0, 1)] private float maxSpdModifier = 0.7f;
    
    private bool _jumpThisFrame;
    private float _lastGroundHeight = UNINITIALIZED_GROUND_HEIGHT;
    private bool _jetting;
    private float _shortPressFlySpd;
    private float _shortPressFlyFinishTime;
    
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
                _shortPressFlyFinishTime = Time.time + flyLongPressDelay;
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
        
        if (_lastGroundHeight == UNINITIALIZED_GROUND_HEIGHT)
        {
            _lastGroundHeight = yValue; // first time prepare is called, record the ground height
            _shortPressFlySpd = shortPressJetHeight / shortPressJetTime;
        }
        
        if (isGrounded && DroppingOrOnGround && !_jetting)
        {
            YSpeed = -0.1f; // press to ground
            _lastGroundHeight = yValue;
            IsJumping = false;
            return new Vector3(0, YSpeed, 0);
        }
        
        if (_jetting || IsShortPressJet) // upwards
        {
            if (yValue - _lastGroundHeight > maxHeight) // ceiling
            {
                YSpeed = 0;
                return Vector3.zero;
            }
            
            YSpeed = IsShortPressJet ? _shortPressFlySpd : longPressJetSpeed;
            return new Vector3(0, YSpeed * Time.fixedDeltaTime, 0);
        } 
        
        if(YSpeed > -maxDropSpd) // falling
        {
            var oldYSpeed = YSpeed;
            YSpeed -= gravity * Time.fixedDeltaTime;
            if (YSpeed < -maxDropSpd) YSpeed = -maxDropSpd; // max drop speed
            
            return new Vector3(0, (YSpeed + oldYSpeed) * 0.5f * Time.fixedDeltaTime,0);
        }
        YSpeed = - maxDropSpd;
        return new Vector3(0, YSpeed * Time.fixedDeltaTime, 0);
    }
    
    private bool IsShortPressJet => Time.time <= _shortPressFlyFinishTime;
    public bool IsLongPressJet => _jetting && Time.time > _shortPressFlyFinishTime; 
    // for fuel consumption, short press uses A amount of fuel, then long press uses B amount per second

    public override void ResetState()
    {
        base.ResetState();
        _jumpThisFrame = false;
        _lastGroundHeight = UNINITIALIZED_GROUND_HEIGHT;
        _jetting = false;
        _shortPressFlySpd = 0;
        _shortPressFlyFinishTime = 0;
    }
}