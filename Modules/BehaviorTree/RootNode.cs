using UnityEngine;

public class RootNode : BTNode
{
    [HideInInspector] public BTNode Child;

    protected override State OnUpdate() => Child.Run();
    // public override void OnFixedUpdate()=>Child.OnFixedUpdate();

    public override BTNode Clone() {
        RootNode node = Instantiate(this);
        node.Child = Child.Clone();
        return node;
    }

    public override string Description() => "Root node";
}