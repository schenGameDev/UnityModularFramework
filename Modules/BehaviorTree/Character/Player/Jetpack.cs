using System;
using UnityEngine;

[Serializable]
public class Jetpack : JumpProcessor
{
    [SerializeField, Min(0)] private float maxDropSpd = 10;
    [SerializeField, Min(0)] private float gravity = 10;
    [SerializeField, Tooltip("x axis is absolute height")] private AnimationCurve jetAcceleration;
    [SerializeField, Range(0, 1)] private float accelerationModifier = 0.5f; // move slower midair
    [SerializeField, Range(0, 1)] private float maxSpdModifier = 0.5f;
    private bool _jumpThisFrame = false;
    public override void StartJump(bool isStarted)
    {
        _jumpThisFrame = isStarted;
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
        if (isGrounded && DroppingOrOnGround && !IsJumping)
        {
            YSpeed = -0.1f; // press to ground
            return new Vector3(0, YSpeed, 0);
        }
        if(YSpeed > -maxDropSpd) // y Speed can increase
        {
            var oldYSpeed = YSpeed;
            YSpeed += ( - gravity + (IsJumping? jetAcceleration.Evaluate(yValue) : 0)) * Time.fixedDeltaTime;
            if(YSpeed < -maxDropSpd) {
                YSpeed = -maxDropSpd; // max drop speed
            }
            return new Vector3(0, (YSpeed + oldYSpeed) * 0.5f * Time.fixedDeltaTime,0);
        }
        
        return new Vector3(0, YSpeed * Time.fixedDeltaTime, 0);
    }
}