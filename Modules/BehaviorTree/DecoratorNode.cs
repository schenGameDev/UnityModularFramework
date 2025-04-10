using UnityEngine;

public abstract class DecoratorNode : BTNode
{
    [HideInInspector] public BTNode Child;

    // public override void OnFixedUpdate()=>Child.OnFixedUpdate();

    public override BTNode Clone() {
        DecoratorNode node = Instantiate(this);
        node.Child = Child.Clone();
        return node;
    }
}
