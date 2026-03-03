using System;
using ModularFramework;
using UnityEngine;

[Serializable]
public abstract class JumpProcessor : IResetable
{
    public abstract float GroundAccelMidair(float groundAccel);
    public abstract float GroundDecelMidair(float groundDecel);
    public abstract float MaxGroundSpeedMidair(float maxGroundSpeed);
    public abstract Vector3 GetVerticalVelocity(float yValue, bool isGrounded);

    public float YSpeed { get; set; } = -0.1f;
    public abstract void StartJump(bool isStarted);
    public bool IsJumping { get; protected set; }
    public bool DroppingOrOnGround => YSpeed <= 0;

    /// <summary>
    /// return true if jump starts this frame
    /// </summary>
    /// <returns></returns>
    public abstract bool Prepare();

    public virtual void ResetState()
    {
        IsJumping = false;
        YSpeed = -0.1f;
    }
}