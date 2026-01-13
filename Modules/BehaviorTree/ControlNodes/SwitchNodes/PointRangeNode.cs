using UnityEngine;

public class PointRangeNode : SwitchNode
{
    public string CenterName,TargetName;
    public Vector2 MinMaxRange;
    public bool OutOfRange = false;

    private float _minSqr,_maxSqr;
    private Transform _targetTf,_centerTf;

    protected override void OnEnter()
    {
        _minSqr = MinMaxRange.x>0? MinMaxRange.x*MinMaxRange.x : 0;
        _maxSqr = MinMaxRange.y>0 && MinMaxRange.y>MinMaxRange.x? MinMaxRange.y*MinMaxRange.y : _minSqr;
        _targetTf = tree.Manager.sensorSystem.Get().GetTransform(TargetName, false);
        _centerTf = tree.Manager.sensorSystem.Get().GetTransform(CenterName,false);
    }
    protected override bool IsSwitchState() {
        if(tree== null) return false;
        return IsTargetInRightRange();
    }

    private bool IsTargetInRightRange() {
        if(_targetTf == null || _centerTf == null) return false;

        var sqrDist = (_targetTf.position - _centerTf.position).sqrMagnitude;

        bool inRange = _minSqr <= sqrDist && sqrDist <= _maxSqr;
        return (!OutOfRange && inRange) || (OutOfRange && !inRange);
    }

    public override string Description() => "Target in/out of point's range";


    public override BTNode Clone()
    {
        var clone = base.Clone() as PointRangeNode;
        clone.CenterName=CenterName;
        clone.TargetName=TargetName;
        clone.MinMaxRange = MinMaxRange;
        clone.OutOfRange = OutOfRange;
        return clone;
    }
}
