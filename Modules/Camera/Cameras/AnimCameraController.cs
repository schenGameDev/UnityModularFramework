using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ModularFramework;

public class AnimCameraController : MonoBehaviour
{
    [SerializeField] private Animator cameraAnimator;
    [SerializeField] private string initialState;
    [SerializeField] private string animState;
    [SerializeField] private EmptyCamera emptyCamera;
    private readonly Autowire<CameraManagerSO> _cameraManager=new();

    int _initStateHash,_animStateHash;

    private void Start() {
        _initStateHash = Animator.StringToHash(initialState);
        _animStateHash = Animator.StringToHash(animState);
    }

    public void UpdateCamFinishPos() {//Animation Event function
        emptyCamera.SaveCurrentPosAndRot();
    }

    void PlayAnim() => cameraAnimator.Play(_animStateHash);
    void ResetAnim() => cameraAnimator.CrossFade(_initStateHash, 0f);

    public void SetExecCamActive(bool active)
    {
        if (active)
        {
            _cameraManager.Get().CameraTransitionTo(emptyCamera.name, CameraTransitionType.NONE);
            PlayAnim();
        }
        else
        {
            _cameraManager.Get().BackToPrevCamera(CameraTransitionType.MATCH_LAST_ROT);
            ResetAnim();
        }
    }

}
