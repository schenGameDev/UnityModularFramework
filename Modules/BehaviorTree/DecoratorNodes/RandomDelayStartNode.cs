using UnityEngine;

public class RandomDelayStartNode : DelayStartNode
{
    public Vector2 MinMaxDuration;

    protected override void OnEnter() {
        base.OnEnter();
        Duration = Random.Range(MinMaxDuration.x, MinMaxDuration.y);
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as RandomDelayStartNode;
        clone.MinMaxDuration = MinMaxDuration;
        return clone;
    }

    public override string Description() => "Child start delay for random duration";
}