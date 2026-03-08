using System;
using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.LockTarget
{
    [DisallowMultipleComponent]
    public class Lockable : MonoBehaviour
    {
        [ReadOnly,SerializeField] private bool locked;

        private readonly Autowire<LockManagerSO> _lockManager = new ();

        private bool _notLockable=false;
    
        private void OnBecameInvisible() {
            if(locked && DisToScreenCenter()>=1) {
                _lockManager.Get().TargetLost();
            }
        }

        public float DisToScreenCenter() {
            if(_notLockable) return 2;

            Vector3 viewPos = SingletonRegistry<GameBuilder>.Instance.MainCamera.WorldToViewportPoint(transform.position);
            bool isInCam = viewPos.x is >= 0 and <= 1 && viewPos.y is >= 0 and <= 1;
            return isInCam? (Mathf.Pow(viewPos.x - 0.5f, 2) +  Mathf.Pow(viewPos.y - 0.5f, 2)) : 1;
        }

        public float HorizontalDisToScreenLeft() {
            Vector3 viewPos = SingletonRegistry<GameBuilder>.Instance.MainCamera.WorldToViewportPoint(transform.position);
            bool isInCam = viewPos.x is >= 0 and <= 1 && viewPos.y is >= 0 and <= 1;
            return isInCam? viewPos.x : 2;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = locked? Color.red : Color.gray;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }

        public void Lock(bool isLock) => locked = isLock;

        public void BecomeUnLockable() { // unlockable but not destroyed yet
            if(locked && _lockManager.Get().lockOnTarget != null) {
                locked = false;
                _lockManager.Get().lockOnTarget = null;
                _lockManager.Get().isLock = false;
            }

            _notLockable = true;
        }

        private void Awake()
        {
            throw new NotImplementedException();
        }

        private void OnEnable()
        {
            Registry<Lockable>.TryAdd(this);
        }

        private void OnDisable()
        {
            Registry<Lockable>.Remove(this);
        }
    }
}

