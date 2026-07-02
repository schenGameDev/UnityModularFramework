using EditorAttributes;
using KBCore.Refs;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    [DisallowMultipleComponent]
    public class BTRunner : MonoBehaviour, ILive, IDelayFacing
    {
        [Required] public BehaviorTreeSO tree;
        [Suffix("s"), SerializeField] private float interval = 0.2f;
        
        public float turnSpeed = 60;
        
        public bool Live { get; set; }
        private float _btTimer = 0f;

#if UNITY_EDITOR
        private void OnValidate() => this.ValidateRefs();
#endif

        private void Start()
        {
            tree = tree.Clone();
            tree.Initialize(transform);
        }

        private void Update()
        {
            if (!Live) return;

            _btTimer += Time.deltaTime;
            if (_btTimer >= interval)
            {
                tree.Run();
                _btTimer = 0f;
            }

            UpdateFaceDirection();
        }

        #region Face Direction

        public Vector3 TargetFacingDirection { get; private set; } // slowly face the direction over time
        private Transform _faceTarget;

        public void FaceTarget(Transform target, bool oneTime = false)
        {
            if (!oneTime)
            {
                _faceTarget = target;
            }
            else
            {
                ResetFace();
            }

            TargetFacingDirection = target.position - transform.position;
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            ResetFace();
            TargetFacingDirection = targetPosition - transform.position;
        }

        public void FaceDirection(Vector3 direction)
        {
            ResetFace();
            TargetFacingDirection = direction;
        }

        public void ResetFace()
        {
            _faceTarget = null;
        }

        private void UpdateFaceDirection()
        {
            if (_faceTarget != null)
            {
                TargetFacingDirection = _faceTarget.position - transform.position;
            }

            if (TargetFacingDirection != Vector3.zero)
            {
                Vector3 targetDirection = new Vector3(TargetFacingDirection.x, 0f, TargetFacingDirection.z);
                if (targetDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    transform.rotation =
                        Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                }
            }
        }

        #endregion

        #region Blackboard

        public void AddParameterToBlackboard(string key, string value) => tree.blackboard.Add(key, value);

        public void AddTransformToBlackboard(string key, Transform tf) => tree.blackboard.Add(key, tf);

        public void RemoveTransformFromBlackboard(string key) => tree.blackboard.RemoveInSceneObject(key);

        public void RemoveParameterFromBlackboard(string key) => tree.blackboard.RemoveParameter(key);

        #endregion
    }
}