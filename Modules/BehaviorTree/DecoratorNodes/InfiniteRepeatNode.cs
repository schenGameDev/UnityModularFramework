public class InfiniteRepeatNode : DecoratorNode
{
    protected override State OnUpdate()
    {
        Child.Run();
        return State.Running;
    }

    public override string Description() => "Repeat infinitely";

    public override BTNode Clone()
    {
        var clone = base.Clone() as InfiniteRepeatNode;
        return clone;
    }
}