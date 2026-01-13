using EditorAttributes;
using UnityEngine;

public class TestTargetInPointRangeNode : SwitchNode
{
    public string centerName,targetName;
    [Clamp(0,999,0,999)] public Vector2 minMaxRange;
    public bool reverse = false;

    private float _minSqr,_maxSqr;
    private Transform _targetTf,_centerTf;

    protected override void OnEnter()
    {
        _minSqr = minMaxRange.x>0? minMaxRange.x*minMaxRange.x : 0;
        _maxSqr = minMaxRange.y>0 && minMaxRange.y>minMaxRange.x? minMaxRange.y*minMaxRange.y : _minSqr;
        _targetTf = tree.blackboard.Get<Transform>(targetName)?[0];
        _centerTf = tree.blackboard.Get<Transform>(centerName)?[0];
    }

    protected override bool Condition()
    {
       return IsTargetInRightRange();
    }

    private bool IsTargetInRightRange() {
        if(_targetTf == null || _centerTf == null) return false;

        var sqrDist = (_targetTf.position - _centerTf.position).sqrMagnitude;

        bool inRange = _minSqr <= sqrDist && sqrDist <= _maxSqr;
        return (!reverse && inRange) || (reverse && !inRange);
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as TestTargetInPointRangeNode;
        clone.centerName=centerName;
        clone.targetName=targetName;
        clone.minMaxRange = minMaxRange;
        clone.reverse = reverse;
        return clone;
    }

    TestTargetInPointRangeNode()
    {
        description = "Target in/out of point's range";
    }
}
