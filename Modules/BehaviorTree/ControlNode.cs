using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControlNode : BTNode
{
    [HideInInspector] public List<BTNode> Children = new();
    protected BTNode currentRunningChild;
    protected virtual int MaxChildrenNum()  => 10;
    public override bool IsNodeChildrenFull() => Children.Count >= MaxChildrenNum();

    // public override void OnFixedUpdate() {
    //     if(currentRunningChild!=null) currentRunningChild.OnFixedUpdate();
    // }

    public override BTNode Clone() {
        ControlNode node = Instantiate(this);
        node.Children = Children.ConvertAll(c=>c.Clone());
        return node;
    }
}
