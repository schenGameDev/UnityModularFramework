using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class ControlNode : BTNode
{
    [HideInInspector] public List<BTNode> Children = new();
    protected BTNode currentRunningChild;
    protected virtual int MaxChildrenNum()  => 10;
    public override bool IsNodeChildrenFull() => Children.Count >= MaxChildrenNum();
    public override List<BTNode> GetChildren() => Children;
    public override bool AddChild(BTNode newChild)
    {
        if (IsNodeChildrenFull() || Children.Contains(newChild)) return false;
        Children.Add(newChild);
        return true;
    }
    
    public override bool RemoveChild(BTNode childToRemove)
    {
        Children.Remove(childToRemove);
        return true;
    }
    
    public override void ClearChildren() => Children.Clear();
    
    public void SortChildren()
    {
        Children.Sort(SortByHorizontalPosition);
    }
    
    private int SortByHorizontalPosition(BTNode left, BTNode right)
    {
        var l = left is SingletonNode s? s.nodePositions.FirstOrDefault(np => np.parentGuid == this.guid) : null;
        var r = right is SingletonNode s2? s2.nodePositions.FirstOrDefault(np => np.parentGuid == this.guid) : null;
        var leftPos = l?.position ?? left.position;
        var rightPos = r?.position ?? right.position;
        
        return leftPos.x < rightPos.x? -1 : 1;
    }

    // public override void OnFixedUpdate() {
    //     if(currentRunningChild!=null) currentRunningChild.OnFixedUpdate();
    // }

    public override BTNode Clone() {
        ControlNode node = Instantiate(this);
        node.Children = Children.ConvertAll(c=>c.Clone());
        return node;
    }
    
    public override void CascadeExit()
    {
        Exit();
        currentRunningChild?.CascadeExit();
    }
    
    public override OutputPortDefinition[] OutputPortDefinitions => new[] { new OutputPortDefinition(Port.Capacity.Multi) };
    public override Color HeaderColor => new Color32(63, 128, 247, 255);
}
