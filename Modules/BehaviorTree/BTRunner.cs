using System;
using EditorAttributes;
using KBCore.Refs;
using ModularFramework;
using UnityEngine;

[DisallowMultipleComponent]
public class BTRunner : MonoBehaviour,ILive
{
    [Required] public BehaviorTreeSO tree;
    [Suffix("s"),SerializeField] private float interval = 0.2f;
    public bool Live { get; set; }
    private Action _onAnimKeyEvent;
    [SerializeField,Self(Flag.Optional)] Animator animator;
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

    #region Animation
    public void PlayAnim(string flag, Action onKeyEvent = null)
    {
        _onAnimKeyEvent = onKeyEvent;
        Invoke(nameof(AnimKeyEvent), 0.5f);
        if (string.IsNullOrEmpty(flag))
        {
            return;
        }
        if(animator!=null) animator.SetBool(flag, true);
        
        Debug.Log($"Playing animation with flags: {flag}");
    }
    
    public void AnimKeyEvent() => _onAnimKeyEvent?.Invoke();
    
    public void StopAnim(string flag)
    {
        if(string.IsNullOrEmpty(flag)) return;
        if(animator!=null) animator.SetBool(flag, false);
        Debug.Log($"Stopping animation with flags: {flag}");
        _onAnimKeyEvent = null;
    }
    #endregion
    
    
    #region Face Direction
    public Vector3 faceDirection; // slowly face the direction over time
    private Transform _faceTarget;

    public void FaceTarget(Transform target, bool oneTime = false) {
        if(!oneTime) {
            _faceTarget = target;
        }
        else
        {
            ResetFace();
        }
        FaceDirection(target.position - transform.position);
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        ResetFace();
        faceDirection = targetPosition - transform.position;
    }

    public void FaceDirection(Vector3 direction) {
        ResetFace();
        faceDirection = direction;
    }
    
    public void ResetFace() {
        _faceTarget = null;
    }
    
    private void UpdateFaceDirection()
    {
        if (_faceTarget != null)
        {
            faceDirection = _faceTarget.position - transform.position;
        }
        
        if (faceDirection != Vector3.zero)
        {
            Vector3 targetDirection = new Vector3(faceDirection.x, 0f, faceDirection.z);
            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation =
                    Quaternion.RotateTowards(transform.rotation, targetRotation, 180f * Time.deltaTime);
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
