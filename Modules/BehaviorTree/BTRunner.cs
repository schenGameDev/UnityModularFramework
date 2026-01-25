using System;
using EditorAttributes;
using ModularFramework;
using UnityEngine;

[DisallowMultipleComponent]
public class BTRunner : MonoBehaviour,ILive
{
    public BehaviorTreeSO tree;
    [Suffix("s"),SerializeField] private float interval = 0.2f;
    public bool Live { get; set; }
    private Action _onAnimKeyEvent;
    private Animator _animator;
    private float _btTimer = 0f;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        tree = tree.Clone();
        tree.Me = transform;
        tree.Initialize();
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
        if(_animator!=null) _animator.SetBool(flag, true);
        
        Debug.Log($"Playing animation with flags: {flag}");
    }
    
    public void AnimKeyEvent() => _onAnimKeyEvent?.Invoke();
    
    public void StopAnim(string flag)
    {
        if(string.IsNullOrEmpty(flag)) return;
        if(_animator!=null) _animator.SetBool(flag, false);
        Debug.Log($"Stopping animation with flags: {flag}");
        _onAnimKeyEvent = null;
    }
    #endregion
    
    
    #region Face Direction
    public Vector3 faceDirection; // slowly face the direction over time
    private Transform _faceTarget;

    public void FaceTarget(Transform target, bool oneTime) {
        if(!oneTime) {
            _faceTarget = target;
        }
        else
        {
            FaceDirection(target.position - transform.position);
        }
        
    }

    public void FaceTarget(Vector3 targetPosition) => FaceDirection(targetPosition - transform.position);
    
    public void FaceDirection(Vector3 direction) {
        if(_faceTarget) return;
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
    
    
    public void AddParameter(string key, string value) {
        tree.blackboard.Add(key, value);
    }
}
