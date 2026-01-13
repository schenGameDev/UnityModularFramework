using ModularFramework;
using UnityEditor.Experimental.GraphView;

public class WaitForEventNode : ControlNode
{
    public EventChannel eventChannel;

    private bool _isTriggered;

    protected override void OnEnter()
    {
        _isTriggered = !eventChannel;

    }

    private void SetTrigger()
    {
        _isTriggered = true;
    } 
    
    protected override int MaxChildrenNum() => 1;
    public override OutputPortDefinition[] OutputPortDefinitions => new[] { new OutputPortDefinition(Port.Capacity.Single) };

    
    private void OnEnable() {
        eventChannel?.AddListener(SetTrigger);
    }

    private void OnDisable() {
        eventChannel?.RemoveListener(SetTrigger);
    }
    
    protected override State OnUpdate()
    {
        if (currentRunningChild is null) return State.Failure;
        
        return _isTriggered ? currentRunningChild.Run() : State.Running;
    }

    public override BTNode Clone()
    {
        var clone = base.Clone() as WaitForEventNode;
        clone.eventChannel = eventChannel;
        return clone;
    }
    
    WaitForEventNode()
    {
        description = "Wait until getting triggered By an external event. Won't be triggered again until re-entered.";
    }
}
