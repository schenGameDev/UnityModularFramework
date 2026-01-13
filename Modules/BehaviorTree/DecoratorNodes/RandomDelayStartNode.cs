using UnityEngine;

public class RandomDelayStartNode : DelayStartNode
{
    public Vector2 minMaxDuration;

    protected override void OnEnter() {
        base.OnEnter();
        duration = Random.Range(minMaxDuration.x, minMaxDuration.y);
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as RandomDelayStartNode;
        clone.minMaxDuration = minMaxDuration;
        return clone;
    }

    RandomDelayStartNode()
    {
        description = "Child delays start for random duration";
    }
    
}