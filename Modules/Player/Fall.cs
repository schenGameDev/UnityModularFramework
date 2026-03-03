using UnityEngine;

namespace UnityModularFramework.Modules.Player
{
    /// <summary>
    /// This is used when fall has different gravity/drop speed from jump landing
    /// </summary>
    public class Fall : JumpProcessor
    {
        [SerializeField, Min(0)] private float maxDropSpd = 10;
        [SerializeField, Min(0)] private float gravity = 10;
        [SerializeField, Range(0, 1)] private float accelerationModifier = 0; // move slower midair
        [SerializeField, Range(0, 1)] private float maxSpdModifier = 0;
        
        
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
            if (isGrounded && DroppingOrOnGround)
            {
                YSpeed = -0.1f; // press to ground
                return new Vector3(0, YSpeed, 0);
            }
            var g = - gravity;
            if(YSpeed > -maxDropSpd)
            {
                YSpeed += g * Time.fixedDeltaTime;
                if(YSpeed < -maxDropSpd) YSpeed = -maxDropSpd; // max drop speed
            }
            
            return new Vector3(0, YSpeed * Time.fixedDeltaTime + 0.5f * g * Time.fixedDeltaTime * Time.fixedDeltaTime, 0);
        }

        public override void StartJump(bool isStarted)
        {
            // do nothing, fall doesn't jump
        }

        public override bool Prepare()
        {
            return false; // can't jump
        } 
    }
}