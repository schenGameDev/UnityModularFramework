using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class DecoratorNode : BTNode
{
    [HideInInspector] public BTNode child;

    // public override void OnFixedUpdate()=>Child.OnFixedUpdate();

    public override BTNode Clone() {
        DecoratorNode node = Instantiate(this);
        node.child = child.Clone();
        return node;
    }
    
    public override OutputPortDefinition[] OutputPortDefinitions => new[] { new OutputPortDefinition(Port.Capacity.Single) };
    public override Color HeaderColor => new Color32(234, 170, 14, 255);
    public override List<BTNode> GetChildren() => child==null? new List<BTNode>() : new List<BTNode> {child};
    public override void ClearChildren() => child = null;
    public override bool AddChild(BTNode newChild)
    {
        child = newChild;
        return true;
    }
    public override bool RemoveChild(BTNode childToRemove)
    {
        if(child != childToRemove) return false;
        child = null;
        return true;
    }

    public override void CascadeExit()
    {
        Exit();
        child?.CascadeExit();
    }
}
