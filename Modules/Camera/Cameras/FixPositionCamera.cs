using UnityEngine;

namespace ModularFramework.Modules.Camera
{
    public class FixPositionCamera : CameraBase
    {
        public FixPositionCamera()
        {
            type = CameraType.FIXED;
        }

        public override bool Ready => true;
        protected override Transform CameraFocusSpawnPoint() => null;
    }
}