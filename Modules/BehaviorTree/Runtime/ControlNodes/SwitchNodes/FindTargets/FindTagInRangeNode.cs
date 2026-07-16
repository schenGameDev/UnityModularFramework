using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using ModularFramework.Modules.Targeting;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class FindTagInRangeNode : FindTargetInRangeNode<Transform>
    {
        [TagDropdown] public string tag;

        private List<Transform> _targets = new();

        protected override void OnEnter()
        {
            _targets.Clear();
            base.OnEnter();
            if (_targets is { Count: > 0 })
            {
                tree.blackboard.Add(BTBlackboard.KEYWORD_TARGET, _targets);
                tree.Log($"Assign {string.Join(",", _targets.Select(t => t.name))} to {BTBlackboard.KEYWORD_TARGET}");
            }
        }

        protected override bool Condition()
        {
            if (btRange == null) return false;
            var tfWithTag = GameObject.FindGameObjectsWithTag(tag).Select(go => go.transform);
            var filteredTargets = ITransformTargetFilter.Filter(tfWithTag, tree.Me, btRange.targetFilters);
            _targets = btRange.targetSelector.GetStrategy(tree.Me)(filteredTargets).ToList();
            var isTagInRange = _targets is { Count: > 0 };
            if (isTagInRange)
            {
                LogCondition($"{tag} target found: " + string.Join(",", _targets.Select(t => t.name)), true);
                return true;
            }
            
            LogCondition($"{tag} target not found.", false);
            return false;
        }


        public override BTNode Clone()
        {
            var clone = base.Clone() as FindTagInRangeNode;
            clone.tag = tag;
            return clone;
        }

        FindTagInRangeNode()
        {
            description = "Find the closest target with Tag in/out of host range\n\n" +
                          "<b>Sends</b>: 'Target' to blackboard";
        }
    }
}