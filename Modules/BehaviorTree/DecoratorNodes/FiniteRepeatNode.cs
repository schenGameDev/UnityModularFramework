public class FiniteRepeatNode : DecoratorNode
{
    public int Times=1;
    private int _count=0;

    protected override void OnEnter() {
        _count = 0;
    }
    protected override State OnUpdate()
    {
        var s = Child.Run();
        if(s!=State.Running) _count++;
        return _count<Times? State.Running : s;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as FiniteRepeatNode;
        clone.Times = Times;
        return clone;
    }

    public override string Description() => "Repeat finite times";
}