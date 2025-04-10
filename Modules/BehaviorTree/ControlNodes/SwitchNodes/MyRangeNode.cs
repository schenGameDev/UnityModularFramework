using System;
using UnityEngine;

public class MyRangeNode : SwitchNode
{
    public string TargetName;
    public Vector2 MinMaxRange;
    [Range(0,180)] public int HalfViewConeAngle = 180;
    public bool SeeThrough = false;
    public bool OutOfRange = false;


    protected override bool IsSwitchState() {
        return IsTargetInRightRange();
    }


    private bool IsTargetInRightRange() {
        if(tree.Me == null) return false;
        Transform targetTf = SeeThrough? tree.Manager.sensorSystem.GetTransformAbsoluteInRange(TargetName, tree.Me, MinMaxRange, HalfViewConeAngle, false) :
            tree.Manager.sensorSystem.GetTransformRaycastInRange(TargetName, tree.Me, MinMaxRange, HalfViewConeAngle, false);
        bool inRange = targetTf;

        return (!OutOfRange && inRange) || (OutOfRange && !inRange);

    }


    public override string Description() => "Target in/out of host range";

    public override BTNode Clone()
    {
        var clone = base.Clone() as MyRangeNode;
        clone.TargetName=TargetName;
        clone.MinMaxRange = MinMaxRange;
        clone.SeeThrough = SeeThrough;
        clone.HalfViewConeAngle = HalfViewConeAngle;
        clone.OutOfRange = OutOfRange;
        return clone;
    }
}
