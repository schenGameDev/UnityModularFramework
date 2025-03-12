using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModularFramework;

public class AnimCameraController : MonoBehaviour
{
    [SerializeField] private Animator _cameraAnimator;
    [SerializeField] private string _initialState;
    [SerializeField] private string _animState;
    [SerializeField] private EmptyCamera _camera;
    private CameraManagerSO _cameraManager;

    int _initStateHash,_animStateHash;

    private void Start() {
        _initStateHash = Animator.StringToHash(_initialState);
        _animStateHash = Animator.StringToHash(_animState);
        _cameraManager = GameRunner.Instance.GetModule<CameraManagerSO>().Get();
    }

    public void UpdateExecCamFinishPos() {//Animation Event function
        _camera.SaveCurrentPosAndRot();
    }

    void PlayAnim() => _cameraAnimator.Play(_animStateHash);
    void ResetAnim() => _cameraAnimator.CrossFade(_initStateHash, 0f);

    public void SetExecCamActive(bool active)
    {
        if (active)
        {
            _cameraManager.CameraTransitionTo(_camera.name, CameraTransitionType.NONE);
            PlayAnim();
        }
        else
        {
            _cameraManager.BackToPrevCamera(CameraTransitionType.MATCH_LAST_ROT);
            ResetAnim();
        }
    }

}
