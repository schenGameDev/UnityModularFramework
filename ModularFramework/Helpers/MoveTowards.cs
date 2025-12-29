using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTowards : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float delay;
    [SerializeField] private bool isLocal;
    private Vector3 _target;
    private bool _isStart; // won't start unless target set
    private float _percentage = 0f;
    private float _delayTimer;

    private void Start() {
        _delayTimer = delay;
    }

    public void SetTarget(Vector3 target) {
        if(_target==target) return;
        _target = target;
        _isStart = true;
        _percentage = 0f;
    }

    public void SetTarget(Vector3 target, float seconds) {
        if(_target==target) return;
        _target = target;
        _isStart = true;
        speed = 1 / seconds;
        _percentage = 0;
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void Move() {
        if(!_isStart || _percentage>=1) return;
        
        if(_delayTimer>0) {
            _delayTimer -= Time.fixedDeltaTime;
            return;
        }
        _percentage += speed * Time.fixedDeltaTime;
        var pos = _percentage>=1? _target : Vector3.Lerp(isLocal? transform.localPosition : transform.position, _target, _percentage);
        if(_percentage>=1) _delayTimer = delay;
        if(isLocal) {
            transform.localPosition = pos;
        } else {
            transform.position = pos;
        }
    }
}
