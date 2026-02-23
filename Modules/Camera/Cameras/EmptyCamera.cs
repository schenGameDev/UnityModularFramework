using ModularFramework.Utility;
using UnityEngine;

namespace ModularFramework.Modules.Camera
{
    public class EmptyCamera : CameraBase
    {
        public EmptyCamera()
        {
            type = CameraType.EMPTY;
        }

        protected override Transform CameraFocusSpawnPoint() => null;

        protected override void CreateFocusPoint()
        {
            DebugUtil.Log("Empty Camera doesn't have focus point");
        }

        public override bool Ready => true;

        public override void OnExit()
        {
            // last cam pos/rot saved externally
            ResetPOV();
            enabled = false;
        }

    }
}