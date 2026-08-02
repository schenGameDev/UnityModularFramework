using UnityEngine;

namespace UnityModularFramework.Modules.Player
{
    public class PlayerAnimation : MonoBehaviour
    {
        private static readonly int MOVE_X = Animator.StringToHash("MoveX");
        private static readonly int MOVE_Y = Animator.StringToHash("MoveY");
        private static readonly int IS_MOVING = Animator.StringToHash("Moving");
        private static readonly int IS_JUMPING = Animator.StringToHash("Jumping");
        
        [SerializeField] private Animator animator;
        [SerializeField] private Vector2EventChannelSO moveChannel;
        [SerializeField] private BoolEventChannelSO sprintChannel;
        [SerializeField] private BoolEventChannelSO jumpChannel;
        
        private bool _isMoving = false;
        private bool _isSprinting = false;
        
        private void OnEnable()
        {
            LinkEventChannels();
        }

        private void OnDisable()
        {
            UnlinkEventChannels();
        }
        
        private void LinkEventChannels()
        {
            moveChannel?.AddListener(OnMove);
            sprintChannel?.AddListener(OnSprint);
            jumpChannel?.AddListener(OnJump);
        }
    
        private void UnlinkEventChannels()
        {
            moveChannel?.RemoveListener(OnMove);
            sprintChannel?.RemoveListener(OnSprint);
            jumpChannel?.RemoveListener(OnJump);
        }

        private void OnMove(Vector2 direction)
        {
            MoveDirection(direction, _isSprinting);
        }

        private void OnSprint(bool isSprinting)
        {
            _isSprinting = isSprinting;
        }

        private void OnJump(bool isJumping)
        {
            animator.SetBool(IS_JUMPING, isJumping);
        }
    
        public void MoveDirection(Vector2 direction, bool isSprinting)
        {
            
            if (direction == Vector2.zero)
            {
                if (!_isMoving) return;
                animator.SetBool(IS_MOVING, false);
                _isMoving = false;
                animator.SetFloat(MOVE_X, 0);
                animator.SetFloat(MOVE_Y, 0);
            }
            else
            {
                if (!_isMoving)
                {
                    animator.SetBool(IS_MOVING, true);
                    _isMoving = true;
                }
                animator.SetFloat(MOVE_X, isSprinting? direction.x * 2 : direction.x);
                animator.SetFloat(MOVE_Y, isSprinting? direction.y * 2 : direction.y);
            }
        }
    }
}