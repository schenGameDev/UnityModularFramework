using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActionNode : BTNode
{
    public override List<BTNode> GetChildren() => new();
    public override bool AddChild(BTNode newChild) => false;
    public override bool RemoveChild(BTNode childToRemove) => false;
    public override void ClearChildren() { }
    public override void CascadeExit() => Exit();

    public override OutputPortDefinition[] OutputPortDefinitions => Array.Empty<OutputPortDefinition>();

    public override Color HeaderColor => new Color32(55, 171, 173, 255);
}
