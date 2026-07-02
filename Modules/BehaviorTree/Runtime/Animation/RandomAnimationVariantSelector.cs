using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    /// <summary>
    /// A <see cref="StateMachineBehaviour"/> that randomly selects an animation variant
    /// each time the owning state is entered. On entry it picks a uniform random integer
    /// in the range [0, <see cref="count"/>) and writes it to the Animator integer
    /// parameter identified by <see cref="parameterName"/>, allowing the Animator
    /// Controller to branch to different sub-states or blend-tree entries per visit.
    /// </summary>
    public class RandomAnimationVariantSelector : StateMachineBehaviour
    {
        [SerializeField] private int count;
        [SerializeField] private string parameterName;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            int selection = Random.Range(0, count);
            animator.SetInteger(parameterName, selection);
        }
    }
    
}

