using ModularFramework.Modules.Ability;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class FollowTargetNode : AstarAINode
    {
        public bool highlightTarget = true;
        public bool exitWhenReached = false;
        private Transform _target;

        protected override void OnEnter()
        {
            base.OnEnter();
            var targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
            if (targets is { Count: > 0 })
            {
                _target = targets[0];
                tree.AI.SetNewTarget(_target, BtMove.speed, true);

                HighlightTarget(true);
                BtMove.Move();
                tree.Log($"Following {_target.name}");
            }
            else
            {
                _target = null;
            }
        }


        protected override State OnUpdate()
        {
            if (_target == null || tree.AI.PathNotFound) return State.Failure;
            if (exitWhenReached && tree.AI.TargetReached) return State.Success;
            return State.Running;
        }

        protected override void OnExit()
        {
            HighlightTarget(false);
            if (_target != null) tree.Log($"Stop following {_target.name}");
            base.OnExit();

        }

        private void HighlightTarget(bool on)
        {
            if (!highlightTarget || _target == null) return;
            if (_target.TryGetComponent<IDamageable>(out var damageable))
                damageable.AimedAtBy(on, tree.Me);
        }

        public override BTNode Clone()
        {
            FollowTargetNode node = Instantiate(this);
            return node;
        }

        FollowTargetNode()
        {
            description = "Follow target \n\n" +
                          "<b>Requires</b>: 'Target' in blackboard";
        }
    }
}