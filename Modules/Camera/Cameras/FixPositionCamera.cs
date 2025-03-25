using UnityEngine;

public class FixPositionCamera : CameraBase
{
    public FixPositionCamera() {
        Type = CameraType.FIXED;
    }

    protected override Transform CameraFocusSpawnPoint() => null;
}