using EditorAttributes;
using UnityEngine;

namespace ModularFramework.Modules.BehaviorTree
{
    public class TestTargetInPointRangeNode : SwitchNode
    {
        public string centerName, targetName;
        [Clamp(0, 999, 0, 999)] public Vector2 minMaxRange;
        public bool reverse = false;

        private float _minSqr, _maxSqr;
        private Transform _targetTf, _centerTf;

        protected override void OnEnter()
        {
            _minSqr = minMaxRange.x > 0 ? minMaxRange.x * minMaxRange.x : 0;
            _maxSqr = minMaxRange.y > 0 && minMaxRange.y > minMaxRange.x ? minMaxRange.y * minMaxRange.y : _minSqr;
            _targetTf = tree.blackboard.Get<Transform>(targetName)?[0];
            _centerTf = tree.blackboard.Get<Transform>(centerName)?[0];
        }

        protected override bool Condition()
        {
            return IsTargetInRightRange();
        }

        private bool IsTargetInRightRange()
        {
            if (_targetTf == null)
            {
                LogCondition($"{targetName} not found in blackboard for testing {title}.", false);
                return false;
            }

            if (_centerTf == null)
            {
                LogCondition($"{centerName} not found in blackboard for testing {title}.", false);
                return false;
            }

            var sqrDist = (_targetTf.position - _centerTf.position).sqrMagnitude;

            bool inRange = _minSqr <= sqrDist && sqrDist <= _maxSqr;
            var isInRightRange = (!reverse && inRange) || (reverse && !inRange);
            LogCondition($"{targetName} in right range relative to {centerName}.", isInRightRange);

            return isInRightRange;
        }

        public override BTNode Clone()
        {
            var clone = base.Clone() as TestTargetInPointRangeNode;
            clone.centerName = centerName;
            clone.targetName = targetName;
            clone.minMaxRange = minMaxRange;
            clone.reverse = reverse;
            return clone;
        }

        TestTargetInPointRangeNode()
        {
            description = "Target in/out of point's range";
        }
    }
}