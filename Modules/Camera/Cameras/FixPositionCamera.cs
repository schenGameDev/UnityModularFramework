using UnityEngine;

public class FixPositionCamera : CameraBase
{
    public FixPositionCamera() {
        type = CameraType.FIXED;
    }

    protected override Transform CameraFocusSpawnPoint() => null;
}