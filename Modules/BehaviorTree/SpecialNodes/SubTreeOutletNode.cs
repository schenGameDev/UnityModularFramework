using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;

public class SubTreeOutletNode : BTNode
{
    [ReadOnly] public SubTreeRootNode subTreeRootNode;

    public override List<BTNode> GetChildren()
    {
        return subTreeRootNode? new() {subTreeRootNode} : new();
    }
    public override bool AddChild(BTNode newChild) => false;
    public override bool RemoveChild(BTNode childToRemove) => false;
    public override void ClearChildren() { }
    
    public override void CascadeExit()
    {
        Exit();
        subTreeRootNode?.CascadeExit();
    }

    public override Color HeaderColor => new Color32(251, 68, 68, 255);
    public override OutputPortDefinition[] OutputPortDefinitions  => Array.Empty<OutputPortDefinition>();
    public override bool HideInputPort() => true;
    
    protected override State OnUpdate()
    {
        if (subTreeRootNode == null)
        {
            Debug.LogError($"Sub Tree Root node {title} could not be found");
            return State.Failure;
        }

        return subTreeRootNode.Run();
    }

    public override BTNode Clone() {
        SubTreeOutletNode node = Instantiate(this);
        return node;
    }

    SubTreeOutletNode()
    {
        description= "Sub tree outlet, redirect to the sub tree root node with the same title\r\n" +
                     "<b>Requires</b>: Title not empty";

    }
}
