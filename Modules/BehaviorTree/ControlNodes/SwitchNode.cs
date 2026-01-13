using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class SwitchNode : ControlNode
{
    public const string PORT_YES = "Y";
    public const string PORT_NO = "N";
    
    [Tooltip("Verify the condition each frame. If it changes, fail the node")]public bool verifyEachFrame;
    
    private bool _enterConditionState;
    
    protected override void OnEnter()
    {
        if (Children.Count == 0) return;

        _enterConditionState = Condition();
        if (_enterConditionState)
        {
            currentRunningChild = GetChildByPortName(PORT_YES)[0];
        } 
        else
        {
            var foundChildren = GetChildByPortName(PORT_NO);
            currentRunningChild = foundChildren.Count > 0 ? foundChildren[0] : null;
        }
    }

    protected abstract bool Condition();
    
    protected override State OnUpdate()
    {
        if (currentRunningChild is null) return State.Failure;
        
        if (verifyEachFrame && Condition() != _enterConditionState)
        {
            CascadeExit();
            return State.Failure;
        }
        return currentRunningChild.Run();
    }
    
    public override BTNode Clone() {
        var node = base.Clone() as SwitchNode;
        node.verifyEachFrame = verifyEachFrame;
        return node;
    }
    
    public override OutputPortDefinition[] OutputPortDefinitions 
        => new[] { new OutputPortDefinition(Port.Capacity.Single, PORT_YES), 
            new OutputPortDefinition(Port.Capacity.Single, PORT_NO) };
    
    public override Color HeaderColor => new Color32(106, 64, 255, 255);
    
    protected override int MaxChildrenNum() => 2;

}
