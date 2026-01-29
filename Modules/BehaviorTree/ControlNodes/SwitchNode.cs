using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class SwitchNode : ControlNode
{
    public const string PORT_YES = "Y";
    public const string PORT_NO = "N";
    
    [Tooltip("Verify the condition each frame. If it changes, fail the node")]public bool verifyEachFrame;
    
    public AfterAction afterAction;

    public enum AfterAction
    {
        NONE,
        RUN_YES_AFTER_NO_SUCCESS,
        RUN_NO_AFTER_YES_SUCCESS,
    }
    
    protected bool enterConditionState;
    
    protected override void OnEnter()
    {
        if (Children.Count == 0) return;

        enterConditionState = Condition();
        if (enterConditionState)
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
        
        if (verifyEachFrame && Condition() != enterConditionState)
        {
            CascadeExit();
            return State.Failure;
        }
        var res = currentRunningChild.Run();
        if (res==State.Success)
        {
            if((afterAction == AfterAction.RUN_YES_AFTER_NO_SUCCESS && currentRunningChild.parentPortName == PORT_NO) ||
               (afterAction == AfterAction.RUN_NO_AFTER_YES_SUCCESS && currentRunningChild.parentPortName == PORT_YES))
            {
                var nextPort = currentRunningChild.parentPortName == PORT_YES ? PORT_NO : PORT_YES;
                var foundChildren = GetChildByPortName(nextPort);
                if (foundChildren.Count > 0)
                {
                    currentRunningChild = foundChildren[0];
                    return State.Running;
                }
            }
        }

        return res;
    }
    
    public override BTNode Clone() {
        var node = base.Clone() as SwitchNode;
        node.verifyEachFrame = verifyEachFrame;
        node.afterAction = afterAction;
        return node;
    }
    
    public override OutputPortDefinition[] OutputPortDefinitions 
        => new[] { new OutputPortDefinition(Port.Capacity.Single, PORT_YES), 
            new OutputPortDefinition(Port.Capacity.Single, PORT_NO) };
    
    public override Color HeaderColor => new Color32(106, 64, 255, 255);
    
    protected override int MaxChildrenNum() => 2;

}
