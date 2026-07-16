using System.Linq;
using ModularFramework.Modules.Targeting;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class TestTargetInRangeNode : SwitchNode
    {
        [SerializeField] private string rangeName;
        private BTRange _btRange;

        public override void Prepare()
        {
            base.Prepare();
            if (!rangeName.IsEmpty())
            {
                _btRange = GetComponentInMe<BTRange>(rangeName);
            }

            if (_btRange == null)
            {
                Debug.LogError($"BTRange of {rangeName} component not found on {tree.Me.name}");
            }
        }

        protected override bool Condition()
        {
            return IsTargetInRange();
        }

        private bool IsTargetInRange()
        {
            var targets = tree.blackboard.Get<Transform>(BTBlackboard.KEYWORD_TARGET);
            if (targets == null || targets.Count == 0)
            {
                LogCondition($"No targets in blackboard for testing {rangeName}.", false);
                return false;
            }
            var targetsInRange = ITransformTargetFilter.Filter(targets, tree.Me, _btRange.targetFilters)?.ToList();
            var isTargetInRange = targetsInRange?.Count > 0;
            
            if (isTargetInRange)
            {
                LogCondition($"{string.Join(", ", targetsInRange.Where(t => t != null).Select(t => t.name))} in {rangeName} range.", true);
            }
            else
            {
                LogCondition($"{string.Join(", ", targets.Where(t => t != null).Select(t => t.name))} not in {rangeName} range.", false);
            }
            targets = targetsInRange;
            return isTargetInRange;
        }

        public override BTNode Clone()
        {
            var clone = base.Clone() as TestTargetInRangeNode;
            clone.rangeName = rangeName;
            return clone;
        }

        public override string ToString()
        {
            return base.ToString() + (string.IsNullOrEmpty(rangeName) || base.ToString().Contains(rangeName)
                ? ""
                : $" ({rangeName})");
        }

        TestTargetInRangeNode()
        {
            description = "Test if target is in/out of host range \n\n" +
                          "<b>Requires</b>: 'Target' in blackboard";
        }
    }
}