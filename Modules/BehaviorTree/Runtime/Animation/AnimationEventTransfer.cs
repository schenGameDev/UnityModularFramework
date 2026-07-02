using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class AnimationEventTransfer : MonoBehaviour
    {
        [SerializeField] private BTAnimation enemyAnimation;

        public void AnimKeyEvent() => enemyAnimation?.AnimKeyEvent();
    }
}