using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RootNode : BTNode
{
    private RootNode()
    {
        description = "Root node";
        titleCustomizable = false;
    }
    
    [HideInInspector] public BTNode child;

    protected override State OnUpdate() => child.Run();
    // public override void OnFixedUpdate()=>Child.OnFixedUpdate();

    public override BTNode Clone() {
        RootNode node = Instantiate(this);
        node.child = child.Clone();
        return node;
    }


    public override OutputPortDefinition[] OutputPortDefinitions => new[] { new OutputPortDefinition(Port.Capacity.Single) };
    public override Color HeaderColor => new Color32(251, 68, 68, 255);
    public override List<BTNode> GetChildren() => child==null? new List<BTNode>() : new List<BTNode> {child};
    public override void ClearChildren() => child = null;

    public override bool AddChild(BTNode newChild)
    {
        child = newChild;
        return true;
    }

    public override bool RemoveChild(BTNode childToRemove)
    {
        if (child != childToRemove) return false;
        child = null;
        return true;
    }
    
    public override void CascadeExit()
    {
        Exit();
        child?.CascadeExit();
    }
}