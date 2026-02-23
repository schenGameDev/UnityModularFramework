using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework.Modules.Camera;
using UnityEngine;
using Void = EditorAttributes.Void;

namespace ModularFramework.Modules.LockTarget
{
    [CreateAssetMenu(fileName = "LockManager_SO", menuName = "Game Module/Target Lock")]
    public class LockManagerSO : GameModule<LockManagerSO>
    {
        [Header("Config")] [SerializeField] private float maxLockDistance = 10;
        [SerializeField] private bool autoLock = true;

        [FoldoutGroup("Event Channels", nameof(lockEvent), nameof(switchLockTargetEvent))] [SerializeField]
        private Void eventChannelGroup;

        [HideInInspector, SerializeField] private EventChannel lockEvent;
        [HideInInspector, SerializeField] private EventChannel<bool> switchLockTargetEvent;

        [Header("Runtime")] [RuntimeObject] private Autowire<CameraManagerSO> _cameraManager = new();
        [ReadOnly, RuntimeObject] public bool isLock;
        [ReadOnly, RuntimeObject] public Transform lockOnTarget;
        [RuntimeObject] private float _sqrMaxLockDistance;

        [SceneRef("PLAYER")] private Transform _player;
        [SceneFlag("LOCK_CAMERA")] private string _lockCameraName;

        public LockManagerSO()
        {
            updateMode = UpdateMode.EVERY_N_FRAME;
        }

        protected override void OnAwake()
        {
        }

        protected override void OnStart()
        {
        }

        protected override void OnUpdate()
        {
            AutoLock();
        }

        protected override void OnLateUpdate()
        {
        }

        protected override void OnSceneDestroy()
        {
        }

        protected override void OnDraw()
        {
        }


        private void OnEnable()
        {
            lockEvent.AddListener(LockUnlock);
            switchLockTargetEvent.AddListener(SwitchTarget);
            _sqrMaxLockDistance = maxLockDistance * maxLockDistance;
        }

        private void OnDisable()
        {
            lockEvent.RemoveListener(LockUnlock);
            switchLockTargetEvent.RemoveListener(SwitchTarget);
        }

        public void LockUnlock()
        {
            if (isLock)
            {
                // unlock
                if (lockOnTarget != null) lockOnTarget.GetComponent<Lockable>().Lock(false);
                lockOnTarget = null;
                _cameraManager.Get().BackToPrevCamera(CameraTransitionType.SMOOTH);
            }
            else
            {
                // lock
                List<(Transform, float)> lockableInCamera = GetLockableInCamera();

                if (lockableInCamera.Count == 0) return;

                lockOnTarget = lockableInCamera[0].Item1;
                lockOnTarget.GetComponent<Lockable>().Lock(true);

                _cameraManager.Get().CameraTransitionTo(_lockCameraName, CameraTransitionType.SMOOTH);
            }

            isLock = !isLock;
        }

        private List<(Transform, float)> GetLockableInCamera()
        {
            return Registry<Lockable>.All
                .Where(l => Vector3.SqrMagnitude(_player.position - l.transform.position) < _sqrMaxLockDistance)
                .Select(l => (l.transform, l.DisToScreenCenter()))
                .Where(t => t.Item2 < 1)
                .OrderBy(t => t.Item2)
                .ToList();
        }

        [RuntimeObject] private bool _isAutoLockedBefore;

        public void AutoLock()
        {
            if (isLock && (lockOnTarget == null ||
                           Vector3.SqrMagnitude(_player.position - lockOnTarget.position) > _sqrMaxLockDistance))
            {
                LockUnlock();
            }
            else if (!isLock && autoLock && !_isAutoLockedBefore)
            {
                LockUnlock();
                if (isLock) _isAutoLockedBefore = true;
            }
            else if (!isLock && _isAutoLockedBefore && GetLockableInCamera().Count == 0)
            {
                _isAutoLockedBefore = false; // reset
            }
        }

        public bool LockTargetExist() => GetLockableInCamera().Count > 0;

#if UNITY_EDITOR
        [Button]
        private void DisplayLockableNames()
        {
            Debug.Log(string.Join(",", Registry<Lockable>.All.Select(x => x.name)));
        }
#endif

        public void TargetLost()
        {
            if (isLock) LockUnlock();
        }

        public void SwitchTarget(bool isRightSide)
        {
            // + right, - left
            if (!isLock || lockOnTarget == null || Registry<Lockable>.Count <= 1) return;
            List<Transform> lockableInCamera = Registry<Lockable>.All
                .Select(l => (l.transform, l.GetComponent<Lockable>().HorizontalDisToScreenLeft()))
                .Where(l => l.Item2 <= 1)
                .OrderBy(l => l.Item2)
                .Select(l => l.Item1)
                .ToList();
            if (lockableInCamera.Count <= 1) return;

            Transform newTarget = null;
            Transform prev = lockableInCamera.Last();
            foreach (var l in lockableInCamera)
            {
                if (!isRightSide && l == lockOnTarget)
                {
                    newTarget = prev;
                    break;
                }
                else if (isRightSide && prev == lockOnTarget)
                {
                    newTarget = l;
                    break;
                }

                prev = l;
            }

            if (newTarget == null && prev == lockOnTarget) newTarget = lockableInCamera[0];

            if (newTarget != null)
            {
                lockOnTarget.GetComponent<Lockable>().Lock(false);
                lockOnTarget = newTarget;
                lockOnTarget.GetComponent<Lockable>().Lock(true);
            }


        }

        private float CalculateDistanceToCamera(Transform lockable)
        {
            return Vector3.Distance(lockable.position,
                SingletonRegistry<GameBuilder>.Instance.MainCamera.transform.position);
        }
    }
}