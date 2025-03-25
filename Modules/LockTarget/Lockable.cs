using System;
using EditorAttributes;
using UnityEngine;

public class Lockable : Sensible
{
    [ReadOnly,SerializeField] private bool _locked;

    private LockManagerSO _lockManager = null;

    private bool _notLockable=false;

    public Lockable()
    {
        registryTypes = new[] {(typeof(SensorManagerSO), 1), (typeof(LockManagerSO), 2)};
    }

    private void OnBecameInvisible() {
        if(_lockManager == null) {
            _lockManager = GetRegistry<LockManagerSO>().Get();
            if(_lockManager == null) {
                return;
            }
        }

        if(_locked && DisToScreenCenter()>=1) {
            _lockManager.TargetLost();
        }
    }

    public float DisToScreenCenter() {
        if(_notLockable) return 2;

        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        bool isInCam = viewPos.x >=0 && viewPos.x <=1 && viewPos.y >=0 && viewPos.y <=1;
        return isInCam? (Mathf.Pow(viewPos.x - 0.5f, 2) +  Mathf.Pow(viewPos.y - 0.5f, 2)) : 1;
    }

    public float HorizontalDisToScreenLeft() {
        Vector3 viewPos = Camera.main.WorldToViewportPoint(transform.position);
        bool isInCam = viewPos.x >=0 && viewPos.x <=1 && viewPos.y >=0 && viewPos.y <=1;
        return isInCam? viewPos.x : 2;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = _locked? Color.red : Color.gray;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }

    public void Lock(bool isLock) => _locked = isLock;

    public void BecomeUnLockable() { // unlockable but not destroyed yet
        if(_locked && _lockManager.LockTarget != null) {
            _locked = false;
            _lockManager.LockTarget = null;
            _lockManager.IsLock = false;
        }

        _notLockable = true;
    }
}
